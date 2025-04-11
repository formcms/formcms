using FormCMS.Activities.Handlers;
using FormCMS.Activities.Services;
using FormCMS.Activities.Workers;
using FormCMS.Infrastructure.Buffers;

namespace FormCMS.Activities.Builders;

public class ActivityBuilder(ILogger<ActivityBuilder> logger)
{
    public static IServiceCollection AddActivity(IServiceCollection services)
    {
        services.AddSingleton<ActivityBuilder>();
        
        services.AddSingleton(new BufferSettings());
        services.AddSingleton<ICountBuffer,MemoryCountBuffer>();
        services.AddSingleton<IStatusBuffer,MemoryStatusBuffer>();
        
        services.AddSingleton(new ActivitySettings(["like","save"], ["share"],["view"]));
        services.AddScoped<IActivityService, ActivityService>();
        services.AddHostedService<BufferFlushWorker>();
        return services;
    }

    public async Task<WebApplication> UseActivity(WebApplication app)
    {
        logger.LogInformation(
            """
            *********************************************************
            Using Activity Services
            *********************************************************
            """);
        using var scope = app.Services.CreateScope();
        var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();
        await activityService.EnsureActivityTables();
 
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("/activities").MapActivityHandler();
        return app;
    }
}