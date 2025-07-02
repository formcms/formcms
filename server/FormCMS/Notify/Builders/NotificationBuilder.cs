using FormCMS.Notify.Handlers;
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
        await scope.ServiceProvider.GetRequiredService<INotificationService>()
            .EnsureNotificationTables();
        
 
        var systemSettings = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(systemSettings.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("notifications").MapNotificationHandler();
        
        return app;
    }
}