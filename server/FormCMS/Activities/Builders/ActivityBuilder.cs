using FormCMS.Activities.Handlers;
using FormCMS.Activities.Models;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.Buffers;
using Microsoft.AspNetCore.Rewrite;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Activities.Builders;

public class ActivityBuilder(ILogger<ActivityBuilder> logger)
{
    private const string FormCmsContentRoot = "/_content/FormCMS";

    public static IServiceCollection AddActivity(IServiceCollection services, bool enableBuffering)
    {
        services.AddSingleton(
            new ActivitySettings(
                EnableBuffering: enableBuffering,
                ToggleActivities: ["like"],
                RecordActivities: ["share"],
                AutoRecordActivities: ["view"],
                Weights: new Dictionary<string, long>
                {
                    { "view", 10 },
                    { "like", 20 },
                    { "share", 30 },
                },
                ReferenceDateTime: new DateTime(2025,1,1),
                HourBoostWeight: 10
            )
        );
        
        services.AddSingleton<ActivityBuilder>();
        services.AddSingleton(new BufferSettings());
        services.AddSingleton<ICountBuffer,MemoryCountBuffer>();
        services.AddSingleton<IStatusBuffer,MemoryStatusBuffer>();

        services.AddScoped<IActivityCollectService, ActivityCollectService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IActivityPlugin, ActivityPlugin>();
        services.AddScoped<ITopItemService, TopItemService>();
        services.AddScoped<IBookmarkService, BookmarkService>();
        
        services.AddHostedService<BufferFlushWorker>();
        return services;
    }

    public async Task<WebApplication> UseActivity(WebApplication app)
    {
        var activitySettings = app.Services.GetRequiredService<ActivitySettings>();
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IActivityCollectService>().EnsureActivityTables();
        await scope.ServiceProvider.GetRequiredService<IBookmarkService>().EnsureBookmarkTables(); 
        
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);

        apiGroup.MapGroup("/activities").MapActivityHandler();
        apiGroup.MapGroup("/bookmarks").MapBookmarkHandler();

        var portalPath = "/portal";
        RegisterHooks();
        AddCmsPortal();
        
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

        void RegisterHooks()
        {
            var attrs = activitySettings
                .AllCountTypes()
                .Select(type => new Attribute(
                    Field: ActivityCounts.ActivityCountField(type),
                    Header: ActivityCounts.ActivityCountField(type),
                    DataType: DataType.Int)
                );

            var registry = app.Services.GetRequiredService<HookRegistry>();
            registry.ExtraQueryFieldEntities.RegisterDynamic("", (IActivityPlugin service, ExtendingEntityArgs args)=>
            {
                var entities = service.ExtendEntities(args.entities);
                return new ExtendingEntityArgs([..entities]);
            });
            
            registry.QueryPostList.RegisterDynamic("*" ,async (IActivityPlugin service, QueryPostListArgs args)=>
            {
                var entity = args.Query.Entity;
                await service.LoadCounts(entity,[..args.Fields], args.RefRecords, CancellationToken.None);
                return args;
            });
        }
    }
}