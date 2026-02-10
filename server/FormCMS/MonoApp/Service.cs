using System.IO.Compression;
using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Builders;

public interface ISystemSetupService
{
    Task<(bool IsReady, bool HasUser)> GetSystemStatus(CancellationToken ct);
    Settings? GetConfig();
    Task UpdateConfig(Settings settings);
    Task SetupSuperAdmin(SuperAdminRequest request, CancellationToken ct);
    Task AddSpa(IFormFile file, string path, string dir);
    Spa[] GetSpas();
    Task DeleteSpa(string path);
}

public class SystemSetupService(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    IWebHostEnvironment environment
) : ISystemSetupService
{
    public async Task<(bool IsReady, bool HasUser)> GetSystemStatus(CancellationToken ct)
    {
        var accountService = serviceProvider.GetService<IAccountService>();
        var hasUser = false;
        if (accountService is not null)
        {
            hasUser = await accountService.HasUser(ct);
        }
        return (SettingsStore.Load() != null, hasUser);
    }

    public Settings? GetConfig()
    {
        EnsurePermission();
        var settings = SettingsStore.Load();
        return settings is null ? null : settings with { MasterPassword = "***" };
    }

    public Task UpdateConfig(Settings settings)
    {
        var old = SettingsStore.Load();
        if (old is not null)
        {
            if (!string.IsNullOrEmpty(old.MasterPassword))
            {
                var hasher = new PasswordHasher<object>();
                var verifyResult = hasher.VerifyHashedPassword(null, old.MasterPassword, settings.MasterPassword);
                if (verifyResult == PasswordVerificationResult.Failed)
                {
                    throw new ResultException("Invalid Master Password");
                }
                // Master password matches, so we bypass EnsurePermission to allow "DB down" recovery.
            }
            else
            {
                EnsurePermission();
            }
        }

        // Hash the master password before saving
        if (!string.IsNullOrEmpty(settings.MasterPassword))
        {
            var hasher = new PasswordHasher<object>();
            settings = settings with { MasterPassword = hasher.HashPassword(null, settings.MasterPassword) };
        }

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<CmsDbContext>();
            _ = settings.DatabaseProvider switch
            {
                DatabaseProvider.Sqlite => optionsBuilder.UseSqlite(settings.ConnectionString),
                DatabaseProvider.Postgres => optionsBuilder.UseNpgsql(settings.ConnectionString),
                DatabaseProvider.SqlServer => optionsBuilder.UseSqlServer(settings.ConnectionString),
                DatabaseProvider.Mysql => optionsBuilder.UseMySql(settings.ConnectionString, ServerVersion.AutoDetect(settings.ConnectionString)),
                _ => throw new Exception("Database provider not found")
            };

            using var context = new CmsDbContext(optionsBuilder.Options);
            if (!context.Database.CanConnect())
            {
                throw new ResultException("Cannot connect to the database with provided settings.");
            }
        }
        catch (Exception ex)
        {
            throw new ResultException($"Database validation failed: {ex.Message}");
        }

        SettingsStore.Save(settings);
        RestartApp();
        return Task.CompletedTask;
    }

    public async Task SetupSuperAdmin(SuperAdminRequest request, CancellationToken ct)
    {
        var accountService = serviceProvider.GetRequiredService<IAccountService>();
        if (await accountService.HasUser(ct))
        {
            throw new ResultException("Super admin already exists.");
        }

        await accountService.EnsureUser(request.Email, request.Password, [Roles.Sa]);
    }

    public async Task AddSpa(IFormFile file, string path, string dir)
    {
        EnsurePermission();

        if (file == null || file.Length == 0)
            throw new ResultException("No file uploaded.");

        if (!Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            throw new ResultException("Only .zip files are allowed.");

        var targetDir = Path.Combine(environment.WebRootPath, dir);
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true);
        }
        Directory.CreateDirectory(targetDir);

        var tempZipPath = Path.GetTempFileName();
        await using (var stream = new FileStream(tempZipPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        try
        {
            await ZipFile.ExtractToDirectoryAsync(tempZipPath, targetDir);
            
            if (!File.Exists(Path.Combine(targetDir, "index.html")))
            {
                var subDirs = Directory.GetDirectories(targetDir);
                foreach (var subDir in subDirs)
                {
                    if (!File.Exists(Path.Combine(subDir, "index.html"))) continue;
                    // Move everything from subDir to targetDir
                    foreach (var subFile in Directory.GetFiles(subDir))
                    {
                        var destFile = Path.Combine(targetDir, Path.GetFileName(subFile));
                        if (File.Exists(destFile)) File.Delete(destFile);
                        File.Move(subFile, destFile);
                    }
                    foreach (var subSubDir in Directory.GetDirectories(subDir))
                    {
                        var destDir = Path.Combine(targetDir, Path.GetFileName(subSubDir));
                        if (Directory.Exists(destDir)) Directory.Delete(destDir, true);
                        Directory.Move(subSubDir, destDir);
                    }
                    Directory.Delete(subDir, true);
                    break;
                }
            }
        }
        finally
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }

        var settings = SettingsStore.Load();
        if (settings != null)
        {
            var spas = settings.Spas?.ToList() ?? new List<Spa>();
            spas.RemoveAll(s => s.Path == path);
            spas.Add(new Spa(path, dir));
            
            var newSettings = settings with { Spas = spas.ToArray() };
            SettingsStore.Save(newSettings);
        }

        RestartApp();
    }

    public Spa[] GetSpas()
    {
        EnsurePermission();
        var settings = SettingsStore.Load();
        return settings?.Spas ?? [];
    }

    public Task DeleteSpa(string path)
    {
        EnsurePermission();
        var settings = SettingsStore.Load();
        if (settings?.Spas == null) return Task.CompletedTask;

        var spa = settings.Spas.FirstOrDefault(s => s.Path == path);
        if (spa == null) return Task.CompletedTask;

        var spas = settings.Spas.ToList();
        spas.Remove(spa);
        
        var newSettings = settings with { Spas = spas.ToArray() };
        SettingsStore.Save(newSettings);

        var targetDir = Path.Combine(environment.WebRootPath, spa.Dir);
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true);
        }

        RestartApp();
        return Task.CompletedTask;
    }

    private void EnsurePermission()
    {
        var profileService = serviceProvider.GetService<IProfileService>();
        if (profileService is not null && !profileService.HasRole(Roles.Sa))
        {
            throw new ResultException("No Permission");
        }
    }

    private void RestartApp()
    {
        Task.Run(async () =>
        {
            await Task.Delay(500);
            lifetime.StopApplication();
        });
    }
}