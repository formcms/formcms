using FormCMS.Activities.Handlers;
using FormCMS.Activities.Models;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Core.Plugins;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.Builders;

namespace FormCMS.Activities.Builders;

public class ActivityBuilder(ILogger<ActivityBuilder> logger)
{
    public static IServiceCollection AddActivity(IServiceCollection services, 
        bool enableBuffering, 
        ShardRouterConfig? userActivityShardConfig = null,
        ShardConfig? countShardConfig = null
        )
    {
        services.AddSingleton(ActivitySettingsExtensions.DefaultActivitySettings with
        {
            EnableBuffering = enableBuffering
        });
        services.AddSingleton<ActivityBuilder>();
        services.AddSingleton(new BufferSettings());
        services.AddSingleton<ICountBuffer, MemoryCountBuffer>();
        services.AddSingleton<IStatusBuffer, MemoryStatusBuffer>();

        services.AddScoped<IActivityCollectService, ActivityCollectService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IActivityQueryPlugin, ActivityQueryPlugin>();
        services.AddScoped<IBookmarkService, BookmarkService>();

        services.AddScoped(sp =>
        {
            //activity module can use the same shard group as cms.
            //it can also use its own database and shard data for scalability
            var defaultShard = sp.GetRequiredService<ShardGroup>();
            return new ActivityContext(
                userActivityShardConfig == null
                    ? new ShardRouter([defaultShard])
                    : sp.CreateShardRouter(userActivityShardConfig),
                countShardConfig == null
                    ? defaultShard
                    : sp.CreateShard(countShardConfig));
        });
        
        services.AddHostedService<BufferFlushWorker>();
        return services;
    }

    public async Task<WebApplication> UseActivity(WebApplication app)
    {
        var activitySettings = app.Services.GetRequiredService<ActivitySettings>();
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("/activities").MapActivityHandler();
        apiGroup.MapGroup("/bookmarks").MapBookmarkHandler();

        app.Services.GetRequiredService<HookRegistry>().RegisterActivityHooks();
        app.Services.GetRequiredService<PluginRegistry>().RegisterActivityPlugins(activitySettings);
        
        using var scope = app.Services.CreateScope();
        var context =scope.ServiceProvider.GetRequiredService<ActivityContext>();
        await context.CountShardGroup.PrimaryDao.EnsureCountTable();
        await context.UserActivityShardRouter.ExecuteAll(async dao =>
        {
            await dao.EnsureActivityTable();
            await dao.EnsureBookmarkTables();
        });

        logger.LogInformation(
            $"""
             *********************************************************
             Using Activity Services
             enable buffering = {activitySettings.EnableBuffering}
             recordActivities = {string.Join(",", activitySettings.CommandRecordActivities)}
             toggleActivities = {string.Join(",", activitySettings.CommandToggleActivities)}
             autoRecordActivities = {string.Join(",", activitySettings.CommandAutoRecordActivities)}
             *********************************************************
             """);
        return app;
    }
}