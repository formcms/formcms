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
        ShardConfig[]? engagementStatusConfigs = null,
        ShardConfig? engagementCountConfig = null
        )
    {
        services.AddSingleton(EngagementSettingsExtensions.DefaultEngagementSettings with
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
            var logger = sp.GetRequiredService<ILogger<EngagementsBuilder>>();

            if (engagementStatusConfigs == null || engagementStatusConfigs.Length == 0)
            {
                logger.LogWarning("No EngagementStatusConfigs provided, using single default shard");
                return new EngagementContext(
                    new ShardRouter([defaultShard]),
                    engagementCountConfig == null ? defaultShard : sp.CreateShard(engagementCountConfig));
            }

            logger.LogInformation($"Creating ShardRouter with {engagementStatusConfigs.Length} shards");
            for (int i = 0; i < engagementStatusConfigs.Length; i++)
            {
                var cfg = engagementStatusConfigs[i];
                logger.LogInformation($"  Shard {i}: {cfg.DatabaseProvider}, Range [{cfg.Start}, {cfg.End}), DB: {cfg.LeadConnStr.Split(';').FirstOrDefault(x => x.Contains("Database"))}");
            }

            return new EngagementContext(
                sp.CreateShardRouter(engagementStatusConfigs),
                engagementCountConfig == null ? defaultShard : sp.CreateShard(engagementCountConfig));
        });
        
        services.AddHostedService<BufferFlushWorker>();
        return services;
    }

    public async Task UseEngagement(WebApplication app,IServiceScope scope)
    {
        var activitySettings = app.Services.GetRequiredService<EngagementSettings>();
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("/engagements").MapEngagementHandler();
        apiGroup.MapGroup("/bookmarks").MapBookmarkHandler();

        app.Services.GetRequiredService<HookRegistry>().RegisterEngagementHooks();;
        app.Services.GetRequiredService<PluginRegistry>().RegisterEngagementPlugins(activitySettings);
        
        var context =scope.ServiceProvider.GetRequiredService<EngagementContext>();
        await context.EngagementCountShardGroup.PrimaryDao.EnsureCountTable();
        await context.EngagementStatusShardRouter.ExecuteAll(async dao =>
        {
            await dao.EnsureEngagementStatusTable();
            await dao.EnsureBookmarkTables();
        });

        logger.LogInformation(
            $"""
             *********************************************************
             Using Engagement Services
             enable buffering = {activitySettings.EnableBuffering}
             shard count = {context.EngagementStatusShardRouter.ShardCount}
             recordActivities = {string.Join(",", activitySettings.CommandRecordActivities)}
             toggleActivities = {string.Join(",", activitySettings.CommandToggleActivities)}
             autoRecordActivities = {string.Join(",", activitySettings.CommandAutoRecordActivities)}
             *********************************************************
             """);
    }
}