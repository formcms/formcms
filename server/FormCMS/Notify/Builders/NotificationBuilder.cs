using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Handlers;
using FormCMS.Notify.Services;
using FormCMS.Utils.Builders;

namespace FormCMS.Notify.Builders;

public class NotificationBuilder(ILogger<NotificationBuilder> logger)
{
    public static IServiceCollection AddNotify(IServiceCollection services,
        ShardRouterConfig? shardManagerConfig = null)
    {
        services.AddSingleton<NotificationBuilder>();
        services.AddScoped<INotificationService,NotificationService>();
        services.AddScoped(sp => new NotificationContext(shardManagerConfig is null
            ? new ShardRouter([sp.GetRequiredService<ShardGroup>()])
            : sp.CreateShardRouter(shardManagerConfig)));

        return services;
    }

    public async Task UseNotification(WebApplication app)
    {
        //handler
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("notifications").MapNotificationHandler();
        
        //db
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ShardGroup>().PrimaryDao.EnsureNotifyTable();
     
        logger.LogInformation(
            $"""
             *********************************************************
             Using Notification Plugin
             *********************************************************
             """); 
    }
}