using FormCMS.Activities.Handlers;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.Cache;
using Microsoft.AspNetCore.Rewrite;

namespace FormCMS.Activities.Builders;

public class ActivityBuilder(ILogger<ActivityBuilder> logger)
{
    private const string FormCmsContentRoot = "/_content/FormCMS";

    public static IServiceCollection AddActivity(IServiceCollection services, bool enableBuffering)
    {
        services.AddSingleton<ActivityBuilder>();
        
        services.AddSingleton<KeyValueCache<long>>(p =>
            new KeyValueCache<long>(p,
                p.GetRequiredService<ILogger<KeyValueCache<long>>>(),
                "EntityMaxRecordId", TimeSpan.FromMinutes(5)));
        
        services.AddSingleton(new BufferSettings());
        services.AddSingleton<ICountBuffer,MemoryCountBuffer>();
        services.AddSingleton<IStatusBuffer,MemoryStatusBuffer>();
        
        services.AddSingleton(new ActivitySettings(enableBuffering,["like"], ["share"],["view"]));
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IBookmarkService, BookmarkService>();
        services.AddHostedService<BufferFlushWorker>();
        return services;
    }

    public async Task<WebApplication> UseActivity(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IActivityService>().EnsureActivityTables();
        await scope.ServiceProvider.GetRequiredService<IBookmarkService>().EnsureBookmarkTables(); 
        
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);

        apiGroup.MapGroup("/activities").MapActivityHandler();
        apiGroup.MapGroup("/bookmarks").MapBookmarkHandler();

        var portalPath = "/portal";
        AddCmsPortal();
        
        var activitySettings = app.Services.GetRequiredService<ActivitySettings>();
        logger.LogInformation(
            $"""
             *********************************************************
             Using Activity Services
             portal Path = {portalPath}
             enable buffering = {activitySettings.EnableBuffering}
             recordActivities = {string.Join("," ,activitySettings.RecordActivities)}
             toggleActivities = {string.Join("," ,activitySettings.ToggleActivities)}
             autoRecordActivities = {string.Join("," ,activitySettings.AutoRecordActivities)}
             *********************************************************
             """);
        return app;

        void AddCmsPortal()
        {
            var webRootFileProvider = app.Services.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
            if (!webRootFileProvider.GetFileInfo(portalPath + "/index.html").Exists)
            {
                var rewriteOptions = app.Services.GetRequiredService<RewriteOptions>();
                portalPath = FormCmsContentRoot + portalPath;
                rewriteOptions.AddRedirect(@"^portal$", portalPath);
            }
            
            app.MapWhen(context => context.Request.Path.StartsWithSegments(portalPath),
                subApp =>
                {
                    subApp.UseRouting();
                    subApp.UseEndpoints(endpoints =>
                    {
                        endpoints.MapFallbackToFile(portalPath, $"{portalPath}/index.html");
                        endpoints.MapFallbackToFile($"{portalPath}/{{*path:nonfile}}",
                            $"{portalPath}/index.html");
                    });
                });
        }
    }
}