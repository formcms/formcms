using FormCMS.Activities.Handlers;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Infrastructure.Buffers;
using FormCMS.Infrastructure.Cache;

namespace FormCMS.Activities.Builders;

public class ActivityBuilder(ILogger<ActivityBuilder> logger)
{
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
        
        services.AddSingleton(new ActivitySettings(enableBuffering,["like","save"], ["share"],["view"]));
        services.AddScoped<IActivityService, ActivityService>();
        services.AddHostedService<BufferFlushWorker>();
        return services;
    }

    public async Task<WebApplication> UseActivity(WebApplication app)
    {

        using var scope = app.Services.CreateScope();
        var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();
        await activityService.EnsureActivityTables();
 
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);

        apiGroup.MapGroup("/activities").MapActivityHandler();

        var activitySettings = app.Services.GetRequiredService<ActivitySettings>();
        logger.LogInformation(
            $"""
             *********************************************************
             Using Activity Services
             enable buffering = {activitySettings.EnableBuffering},
             recordActivities = {string.Join("," ,activitySettings.RecordActivities)}
             toggleActivities = {string.Join("," ,activitySettings.ToggleActivities)}
             autoRecordActivities = {string.Join("," ,activitySettings.AutoRecordActivities)}
             *********************************************************
             """);
        return app;
    }
}