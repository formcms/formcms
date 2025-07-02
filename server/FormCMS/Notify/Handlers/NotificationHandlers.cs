using FormCMS.Notify.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Notify.Handlers;

public static class NotificationHandlers
{
    public static RouteGroupBuilder MapNotificationHandler(this RouteGroupBuilder builder)
    {
        builder.MapGet("/", (
            INotificationService s,
            HttpContext context,
            int? offset,
            int? limit,
            CancellationToken ct
        ) => s.List(QueryHelpers.ParseQuery(context.Request.QueryString.Value),offset,limit,ct));
        
        builder.MapGet("/unread", (
            INotificationService s,
            CancellationToken ct
        ) => s.UnreadCount(ct));
        return builder;
    }
}