using FormCMS.Auth.Models;
using FormCMS.Cms.Builders;
using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.MonoApp;

public static class Builder
{
    private const string MonoCors = "MonoCors";
    public static MonoSettings? AddMonoApp(this IHostApplicationBuilder builder,string dataPath)
    {
        
        var monoRuntime = new MonoRunTime();
        var storePath = Directory.GetCurrentDirectory();
        if (!string.IsNullOrWhiteSpace(dataPath))
        {
            storePath = Path.Combine(dataPath, "config");
            monoRuntime.AppRoot = Path.Combine(dataPath, "apps");
        }
        
        var settingsStore = new SettingsStore(storePath);
        Console.WriteLine("---------------------------------");
        Console.WriteLine($"dataPath: {dataPath}");
        Console.WriteLine($"storePath: {storePath}");
        Console.WriteLine($"appPath: {monoRuntime.AppRoot}");
        Console.WriteLine("---------------------------------");
        
        builder.Services.AddSingleton(monoRuntime);
        builder.Services.AddSingleton(settingsStore);
        builder.Services.AddScoped<ISystemSetupService, SystemSetupService>();
        
        var monoMonoSettings = settingsStore.Load();
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                MonoCors,
                policy =>
                {
                    var origins = monoMonoSettings?.CorsOrigins?.Select(x => x.Trim('/')) 
                                  ??[];
                    policy.WithOrigins(origins.ToArray())
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        }); 

        if (monoMonoSettings is null)
        {
            return null;
        }        
        
        builder.Services.AddSingleton(monoMonoSettings);
        
        builder.Services.AddScoped<ISpaService, SpaService>();
        builder.AddDbContext(monoMonoSettings.DatabaseProvider,monoMonoSettings.ConnectionString);
        builder.AddOutputCachePolicy();
        CmsBuilder.AddCms(builder.Services, monoMonoSettings.DatabaseProvider, monoMonoSettings.ConnectionString, settings =>
        {
            settings.MapCmsHomePage = monoMonoSettings.Spas?.FirstOrDefault(x => x.Path == "/") == null;
            if (!string.IsNullOrWhiteSpace(dataPath))
            {
                settings.LocalFileStoreOptions.PathPrefix = Path.Combine(dataPath,"files") ;
                
                Console.WriteLine("---------------------------------");
                Console.WriteLine($"pathPrefix: {settings.LocalFileStoreOptions.PathPrefix}");
                Console.WriteLine("---------------------------------");
            }
        });
        
        CmsWorkerBuilder.AddWorker(builder.Services, monoMonoSettings.DatabaseProvider, monoMonoSettings.ConnectionString,
            new TaskTimingSeconds(300,300,300,300));
        builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(new AuthConfig());
        builder.Services.AddEngagement();
        builder.Services.AddComments();
        builder.Services.AddAuditLog();
        return  monoMonoSettings;
    }

    public static void UseMonoCors(this WebApplication app)
    {
        app.UseCors(MonoCors);
    }
    
    public static async Task<bool> EnsureDbCreatedAsync(this IHost app)
    {
        var monoSettings = app.Services.GetRequiredService<MonoSettings>();
        try
        {
            using var scope = app.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            await ctx.Database.EnsureCreatedAsync();
            if (monoSettings?.DatabaseProvider == DatabaseProvider.Sqlite)
            {
                await ctx.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    
    static void AddDbContext(this IHostApplicationBuilder builder, 
        DatabaseProvider dbProvider, string dbConnStr)
    {
        _ = dbProvider switch
        {
            DatabaseProvider.Sqlite=> builder.Services.AddDbContext<CmsDbContext>(options =>
                options.UseSqlite(dbConnStr)),
            DatabaseProvider.Postgres => builder.Services.AddDbContext<CmsDbContext>(options =>
                options.UseNpgsql(dbConnStr)),
            DatabaseProvider.SqlServer => builder.Services.AddDbContext<CmsDbContext>(options =>
                options.UseSqlServer(dbConnStr)),
            DatabaseProvider.Mysql => builder.Services.AddDbContext<CmsDbContext>(options =>
                options.UseMySql( dbConnStr, ServerVersion.AutoDetect(dbConnStr))),
            _ => throw new Exception("Database provider not found")
        };
    }
    
    static void AddOutputCachePolicy(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOutputCache(cacheOption =>
        {
            cacheOption.AddBasePolicy(policyBuilder => policyBuilder.Expire(TimeSpan.FromMinutes(1)));
            cacheOption.AddPolicy(SystemSettings.PageCachePolicyName,
                b => b.Expire(TimeSpan.FromMinutes(1)));
            cacheOption.AddPolicy(SystemSettings.QueryCachePolicyName,
                b => b.Expire(TimeSpan.FromSeconds(1)));
        });
    }
}