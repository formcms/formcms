using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using Humanizer;

namespace FormCMS.Notify.Services;

public class NotificationCollectService(
    IRelationDbDao dao,
    KateQueryExecutor executor
):INotificationCollectService
{
    public async Task Insert(Notification notification, CancellationToken ct)
    {
        await executor.Exec(notification.Insert(), false,ct);
        var condition = new Dictionary<string,object>
        {
            [nameof(NotificationCount.UserId).Camelize()] = notification.UserId
        };
        await dao.Increase(
            NotificationCountExtensions.TableName, 
            condition,
            nameof(NotificationCount.UnreadCount).Camelize(),0,1
            ,ct);
    }
}