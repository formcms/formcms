using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Cms.Builders;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Builders;


public record MonolithicCmsSettings(
    DatabaseProvider DatabaseProvider,
    string ConnectionString
);

public static class MonolithicCmsBuilder
{
    public static void AddMonolithicCms(this IHostApplicationBuilder builder)
    {
        var settings = FormCmsSettingsStore.Load();
        if (settings is null)
        {
            return;
            
        }
        builder.AddDbContext(settings.DatabaseProvider,settings.ConnectionString);
        builder.AddOutputCachePolicy();
        CmsBuilder.AddCms(builder.Services, settings.DatabaseProvider, settings.ConnectionString);
        CmsWorkerBuilder.AddWorker(builder.Services, settings.DatabaseProvider, settings.ConnectionString,
            new TaskTimingSeconds(60,60,60,60));
        builder.Services.AddCmsAuth<CmsUser, IdentityRole, CmsDbContext>(new AuthConfig());
        builder.Services.AddAuditLog();
    }
    
    public static async Task<bool> EnsureDbCreatedAsync(this IHost app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            await ctx.Database.EnsureCreatedAsync();
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
    public static void MapConfigEndpoints(this WebApplication app)
    {
        app.MapGet("/api/system/is-ready",
            () => FormCmsSettingsStore.Load() != null); 

        app.MapGet("/api/system/config",
            (IProfileService profileService) => !profileService.HasRole(Roles.Sa)
                ? throw new ResultException("No Permission")
                : FormCmsSettingsStore.Load());
        
        app.MapPut("/api/system/config", (
            IProfileService profileService,
                IHostApplicationLifetime lifetime,
            [FromBody] MonolithicCmsSettings settings
            ) =>
        {
            var old = FormCmsSettingsStore.Load();
            if (old is not null && !profileService.HasRole(Roles.Sa))
            {
                throw new ResultException("No Permission");
            }
            FormCmsSettingsStore.Save(settings);
            Task.Run(async () =>
            {
                await Task.Delay(500);
                lifetime.StopApplication();
            });
            return Task.FromResult(Results.Ok());
        });
    } 
  
}