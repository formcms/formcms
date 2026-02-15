using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Cms.Builders;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.RelationDbDao;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Builders;

public static class Builder
{
    public static void AddMonoApp(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ISystemSetupService, SystemSetupService>();
        builder.Services.AddScoped<ISpaService, SpaService>();
        var monoSettings = SettingsStore.Load();
        if (monoSettings is null)
        {
            return;
        }
        builder.AddDbContext(monoSettings.DatabaseProvider,monoSettings.ConnectionString);
        builder.AddOutputCachePolicy();
        CmsBuilder.AddCms(builder.Services, monoSettings.DatabaseProvider, monoSettings.ConnectionString, settings =>
        {
            settings.MapCmsHomePage = monoSettings.Spas != null && monoSettings.Spas.FirstOrDefault(x => x.Path == "/") == null;
            var store = builder.Configuration.GetValue<string>("FORMCMS_STORE_PATH");
            if (!string.IsNullOrWhiteSpace(store))
            {
                settings.LocalFileStoreOptions.PathPrefix = store;
            }
        });
        CmsWorkerBuilder.AddWorker(builder.Services, monoSettings.DatabaseProvider, monoSettings.ConnectionString,
            new TaskTimingSeconds(300,300,300,300));
        builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(new AuthConfig());
        builder.Services.AddEngagement();
        builder.Services.AddComments();
        builder.Services.AddAuditLog();
    }
    
    public static async Task<bool> EnsureDbCreatedAsync(this IHost app)
    {
        var monoSettings = SettingsStore.Load();
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