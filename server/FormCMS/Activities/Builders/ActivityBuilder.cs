using FormCMS.Activities.Handlers;
using FormCMS.Activities.Models;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Core.Plugins;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Rewrite;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Activities.Builders;

public class ActivityBuilder(ILogger<ActivityBuilder> logger)
{
    private const string FormCmsContentRoot = "/_content/FormCMS";

    public static IServiceCollection AddActivity(IServiceCollection services, bool enableBuffering)
    {
        services.AddSingleton(ActivitySettingsExtensions.DefaultActivitySettings with
        {
            EnableBuffering = enableBuffering
        });
        services.AddSingleton<ActivityBuilder>();
        services.AddSingleton(new BufferSettings());
        services.AddSingleton<ICountBuffer,MemoryCountBuffer>();
        services.AddSingleton<IStatusBuffer,MemoryStatusBuffer>();

        services.AddScoped<IActivityCollectService, ActivityCollectService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IActivityQueryPlugin, ActivityQueryPlugin>();
        services.AddScoped<IBookmarkService, BookmarkService>();
        
        services.AddHostedService<BufferFlushWorker>();
        return services;
    }

    public async Task<WebApplication> UseActivity(WebApplication app)
    {
        var activitySettings = app.Services.GetRequiredService<ActivitySettings>();
        var registry = app.Services.GetRequiredService<PluginRegistry>();
        registry.PluginQueries.Add(ActivityQueryPluginConstants.TopList);
        foreach (var type in activitySettings.AllCountTypes())
        {
            var field = ActivityCounts.ActivityCountField(type); 
            registry.PluginAttributes[field] = new Attribute(
                Field: field,
                Header: field,
                DataType: DataType.Int);
        }
        
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IActivityCollectService>().EnsureActivityTables();
        await scope.ServiceProvider.GetRequiredService<IBookmarkService>().EnsureBookmarkTables(); 
        
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        

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
             recordActivities = {string.Join("," ,activitySettings.CommandRecordActivities)}
             toggleActivities = {string.Join("," ,activitySettings.CommandToggleActivities)}
             autoRecordActivities = {string.Join("," ,activitySettings.CommandAutoRecordActivities)}
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
            var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
            hookRegistry.PlugInQueryArgs.RegisterDynamic(ActivityQueryPluginConstants.TopList, async (IActivityQueryPlugin s,PlugInQueryArgs args) =>
            {
                var pg = PaginationHelper.ToValid(args.Pagination, 10);
                if (!args.Args.TryGetValue(ActivityQueryPluginConstants.EntityName,out var entityName))
                {
                    throw new ResultException("Entity name is not provided");
                }
                var items = await s.GetTopList(entityName ,pg.Offset,pg.Limit,CancellationToken.None);
                return args with { OutRecords = items };
            });
            
            hookRegistry.QueryPostList.RegisterDynamic("*" ,async (IActivityQueryPlugin service, QueryPostListArgs args)=>
            {
                var entity = args.Query.Entity;
                await service.LoadCounts(entity, [..args.Query.Selection], args.RefRecords, CancellationToken.None);
                return args;
            });
            hookRegistry.QueryPostSingle.RegisterDynamic("*" ,async (IActivityQueryPlugin service, QueryPostSingleArgs args)=>
            {
                var entity = args.Query.Entity;
                await service.LoadCounts(entity, [..args.Query.Selection], [args.RefRecord], CancellationToken.None);
                return args;
            });
            hookRegistry.QueryPostPartial.RegisterDynamic("*",
                async (IActivityQueryPlugin service, QueryPostPartialArgs args) =>
                {
                    var attr = args.Node.LoadedAttribute;
                    if (attr.DataType.IsCompound())
                    {
                        var desc = attr.GetEntityLinkDesc().Ok();
                        await service.LoadCounts(desc.TargetEntity, [..args.Node.Selection], args.RefRecords,
                            CancellationToken.None);
                    }
                    return args;
                });
        }
    }
}