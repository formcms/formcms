using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using Humanizer;

namespace FormCMS.Notify.Services;

public class NotificationCollectService(
    NotificationContext ctx
):INotificationCollectService
{
    public async Task Insert(Notification notification, CancellationToken ct)
    {
        var userId = notification.UserId;
        await ctx.ShardRouter.PrimaryDao(userId).Exec(notification.Insert(), false,ct);
        var condition = new Dictionary<string,object>
        {
            [nameof(NotificationCount.UserId).Camelize()] = notification.UserId
        };
        await ctx.ShardRouter.PrimaryDao(userId).Increase(
            NotificationCountExtensions.TableName, 
            condition,
            nameof(NotificationCount.UnreadCount).Camelize(),0,1
            ,ct);
    }
}