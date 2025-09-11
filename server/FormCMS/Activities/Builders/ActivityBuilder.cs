using FormCMS.Activities.Handlers;
using FormCMS.Activities.Models;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Core.Plugins;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.RelationDbDao;
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
        services.AddSingleton<ICountBuffer, MemoryCountBuffer>();
        services.AddSingleton<IStatusBuffer, MemoryStatusBuffer>();

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
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("/activities").MapActivityHandler();
        apiGroup.MapGroup("/bookmarks").MapBookmarkHandler();

        app.Services.GetRequiredService<HookRegistry>().RegisterActivityHooks();
        app.Services.GetRequiredService<PluginRegistry>().RegisterActivityPlugins(activitySettings);
        
        using var scope = app.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
        await migrator.EnsureActivityTables();
        await migrator.EnsureBookmarkTables();

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