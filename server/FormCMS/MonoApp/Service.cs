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
    Task<(bool DatabaseReady, bool HasMasterPassword, bool HasUser)> GetSystemStatus(CancellationToken ct);
    Settings? GetConfig(string masterPassword);
    Task UpdateDatabaseConfig(DatabaseProvider databaseProvider, string connectionString, string masterPassword);
    Task UpdateMasterPassword(string masterPassword, string? oldMasterPassword = null);
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
    public async Task<(bool DatabaseReady, bool HasMasterPassword, bool HasUser)> GetSystemStatus(CancellationToken ct)
    {
        var settings = SettingsStore.Load();
        
        // Check if database is configured
        bool databaseReady = settings is not null;
        
        // Check if master password is set
        bool hasMasterPassword = !string.IsNullOrWhiteSpace(settings?.MasterPassword);
        
        // Check if user exists (only if database is ready)
        bool hasUser = false;
        if (databaseReady)
        {
            var accountService = serviceProvider.GetRequiredService<IAccountService>();
            hasUser = await accountService.HasUser(ct);
        }

        return (databaseReady, hasMasterPassword, hasUser);
    }

    public Settings? GetConfig(string masterPassword)
    {
        VerifyMasterPassword(masterPassword);
        var settings = SettingsStore.Load();
        if (settings is null) return null;
        
        // Mask sensitive fields before returning
        return settings with { MasterPassword = "***" };
    }

    public Task UpdateDatabaseConfig(DatabaseProvider databaseProvider, string connectionString, string masterPassword)
    {
        VerifyMasterPassword(masterPassword);
        
        // Load existing settings or create new
        var settings = SettingsStore.Load() ?? new Settings(
            DatabaseProvider: databaseProvider,
            ConnectionString: connectionString,
            MasterPassword: ""
        );

        // Update only database fields
        settings = settings with
        {
            DatabaseProvider = databaseProvider,
            ConnectionString = connectionString
        };

        // Validate database connection
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<CmsDbContext>();
            _ = databaseProvider switch
            {
                DatabaseProvider.Sqlite => optionsBuilder.UseSqlite(connectionString),
                DatabaseProvider.Postgres => optionsBuilder.UseNpgsql(connectionString),
                DatabaseProvider.SqlServer => optionsBuilder.UseSqlServer(connectionString),
                DatabaseProvider.Mysql => optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
                _ => throw new Exception("Database provider not found")
            };

            using (var context = new CmsDbContext(optionsBuilder.Options))
            {
                context.Database.CanConnect();
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

    public Task UpdateMasterPassword(string masterPassword, string? oldMasterPassword = null)
    {
        // Load existing settings
        var settings = SettingsStore.Load();
        if (settings is null)
        {
            throw new ResultException("Cannot set master password before database is configured");
        }

        // If a master password already exists, verify the old one first
        if (!string.IsNullOrWhiteSpace(settings.MasterPassword))
        {
            if (string.IsNullOrWhiteSpace(oldMasterPassword))
            {
                throw new ResultException("Current master password is required to change it");
            }
            VerifyMasterPassword(oldMasterPassword);
        }

        // Hash the new master password
        var hasher = new PasswordHasher<object>();
        var hashedPassword = hasher.HashPassword(null, masterPassword);

        // Update only master password field
        settings = settings with { MasterPassword = hashedPassword };

        SettingsStore.Save(settings);
        RestartApp();
        return Task.CompletedTask;
    }

    public async Task SetupSuperAdmin(SuperAdminRequest request, CancellationToken ct)
    {
        // Verify master password before creating super admin
        VerifyMasterPassword(request.MasterPassword);
        
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

    private void VerifyMasterPassword(string masterPassword)
    {
        var settings = SettingsStore.Load();
        if (settings is null || string.IsNullOrWhiteSpace(settings.MasterPassword))
        {
            throw new ResultException("Master password is not configured");
        }

        var hasher = new PasswordHasher<object>();
        var result = hasher.VerifyHashedPassword(null, settings.MasterPassword, masterPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new ResultException("Invalid master password");
        }
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