using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Engagements.Models;
using FormCMS.Engagements.Services;
using FormCMS.Engagements.Workers;
using FormCMS.Engagements.Handlers;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.Builders;

namespace FormCMS.Engagements.Builders;

public class EngagementsBuilder(ILogger<EngagementsBuilder> logger)
{
    public static IServiceCollection AddEngagement(IServiceCollection services, 
        bool enableBuffering, 
        ShardRouterConfig? userActivityShardConfig = null,
        ShardConfig? countShardConfig = null
        )
    {
        services.AddSingleton(ActivitySettingsExtensions.DefaultEngagementSettings with
        {
            EnableBuffering = enableBuffering
        });
        services.AddSingleton<EngagementsBuilder>();
        services.AddSingleton(new BufferSettings());
        services.AddSingleton<ICountBuffer, MemoryCountBuffer>();
        services.AddSingleton<IStatusBuffer, MemoryStatusBuffer>();

        services.AddScoped<IEngagementCollectService, EngagementsCollectService>();
        services.AddScoped<IEngagementService, EngagementService>();
        services.AddScoped<IEngagementQueryPlugin, EngagementQueryPlugin>();
        services.AddScoped<IBookmarkService, BookmarkService>();

        services.AddScoped(sp =>
        {
            //activity module can use the same shard group as cms.
            //it can also use its own database and shard data for scalability
            var defaultShard = sp.GetRequiredService<ShardGroup>();
            return new EngagementContext(
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

    public async Task UseEngagement(WebApplication app)
    {
        var activitySettings = app.Services.GetRequiredService<EngagementSettings>();
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("/engagements").MapActivityHandler();
        apiGroup.MapGroup("/bookmarks").MapBookmarkHandler();

        app.Services.GetRequiredService<HookRegistry>().RegisterActivityHooks();
        app.Services.GetRequiredService<PluginRegistry>().RegisterActivityPlugins(activitySettings);
        
        using var scope = app.Services.CreateScope();
        var context =scope.ServiceProvider.GetRequiredService<EngagementContext>();
        await context.CountShardGroup.PrimaryDao.EnsureCountTable();
        await context.UserActivityShardRouter.ExecuteAll(async dao =>
        {
            await dao.EnsureEngagementStatusTable();
            await dao.EnsureBookmarkTables();
        });

        logger.LogInformation(
            $"""
             *********************************************************
             Using Engagement Services
             enable buffering = {activitySettings.EnableBuffering}
             recordActivities = {string.Join(",", activitySettings.CommandRecordActivities)}
             toggleActivities = {string.Join(",", activitySettings.CommandToggleActivities)}
             autoRecordActivities = {string.Join(",", activitySettings.CommandAutoRecordActivities)}
             *********************************************************
             """);
    }
}