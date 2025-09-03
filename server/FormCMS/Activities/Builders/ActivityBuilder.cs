using FormCMS.Activities.Handlers;
using FormCMS.Activities.Models;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Core.Plugins;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Activities.Builders;

public class ActivityBuilder(ILogger<ActivityBuilder> logger)
{
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
        var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
        var dao = scope.ServiceProvider.GetRequiredService<IRelationDbDao>();
        await EnsureActivityTables();
        await EnsureBookmarkTables();
        
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        

        apiGroup.MapGroup("/activities").MapActivityHandler();
        apiGroup.MapGroup("/bookmarks").MapBookmarkHandler();

        var portalPath = "/portal";
        RegisterHooks();
        
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
        
        async Task EnsureBookmarkTables()
        {
            await migrator.MigrateTable(BookmarkFolders.TableName, BookmarkFolders.Columns);
            await migrator.MigrateTable(Bookmarks.TableName, Bookmarks.Columns);

            await dao.CreateForeignKey(
                Bookmarks.TableName, nameof(Bookmark.FolderId).Camelize(),
                BookmarkFolders.TableName, nameof(BookmarkFolder.Id).Camelize(),
                CancellationToken.None
            );
        }
        async Task EnsureActivityTables()
        {
            await migrator.MigrateTable(Models.Activities.TableName, Models.Activities.Columns);
            await dao.CreateIndex(Models.Activities.TableName, Models.Activities.KeyFields, true, CancellationToken.None);

            await migrator.MigrateTable(ActivityCounts.TableName, ActivityCounts.Columns);
            await dao.CreateIndex(ActivityCounts.TableName, ActivityCounts.KeyFields, true, CancellationToken.None);
        }
        void RegisterHooks()
        {
            var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
            hookRegistry.ListPlugInQueryArgs.RegisterDynamic(ActivityQueryPluginConstants.TopList, async (IActivityQueryPlugin s,ListPlugInQueryArgs args) =>
            {
                var pg = PaginationHelper.ToValid(args.Pagination, 10);
                if (args.Args.TryGetValue(ActivityQueryPluginConstants.EntityName,out var entityName))
                {
                    var items = await s.GetTopList(entityName.ToString() ,pg.Offset,pg.Limit,CancellationToken.None);
                    args = args with { OutRecords = items };
                }

                return args;
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