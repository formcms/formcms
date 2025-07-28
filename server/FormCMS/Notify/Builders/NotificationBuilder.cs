using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Handlers;
using FormCMS.Notify.Models;
using FormCMS.Notify.Services;

namespace FormCMS.Notify.Builders;

public class NotificationBuilder(ILogger<NotificationBuilder> logger)
{
    public static IServiceCollection AddNotify(IServiceCollection services)
    {
        services.AddSingleton<NotificationBuilder>();
        services.AddScoped<INotificationService,NotificationService>();
        return services;
    }

    public async Task<WebApplication> UseNotification(WebApplication app)
    {
        logger.LogInformation(
            $"""
             *********************************************************
             Using Notification Plugin
             *********************************************************
             """);

        using var scope = app.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
        await migrator.MigrateTable(Notifications.TableName, Notifications.Columns);
        await migrator.MigrateTable(NotificationCountExtensions.TableName, NotificationCountExtensions.Columns);
 
 
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("notifications").MapNotificationHandler();
        
        return app;
    }
}