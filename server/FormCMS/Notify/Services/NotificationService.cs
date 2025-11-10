using FormCMS.Cms.Services;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Notify.Services;
public record NotificationContext(ShardRouter UserNotificationShardRouter);

public class NotificationService(
    NotificationContext ctx,
    IIdentityService  identityService,
    IUserManageService userManageService
    ):INotificationService
{
    public async Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var followKate = ctx.UserNotificationShardRouter.ReplicaDao(userId);
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = Notifications.List(userId, offset, limit);
        var items = await followKate.Many(query, Notifications.Columns,filters,sorts,ct);
        await LoadSender(items, ct);
        
        var countQuery = Notifications.Count(userId);
        var count = await followKate.Count(countQuery,Notifications.Columns,filters,ct);

        var leadKate = ctx.UserNotificationShardRouter.PrimaryDao(userId);
        await leadKate.Exec(Notifications.ReadAll(userId), ct);
        await leadKate.Exec(NotificationCountExtensions.ResetCount(userId), ct);
        
        return new ListResponse(items,count); 
    }
    
    public  Task<long> UnreadCount(CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User not logged in");
        var query = NotificationCountExtensions.UnreadCount(userId);
        return ctx.UserNotificationShardRouter.ReplicaDao(userId).ReadScalar(query, ct);
    }

    private async Task LoadSender(Record[] notifications,CancellationToken ct)
    {
        var ids = notifications
            .Select(x => x.StrOrEmpty(nameof(Notification.SenderId).Camelize()))
            .ToArray();
        var users = await userManageService.GetPublicUserInfos(ids, ct);
        var dict = users.ToDictionary(x => x.Id, x => x);
        foreach (var notification in notifications)
        {
            var userId = notification.StrOrEmpty(nameof(Notification.SenderId).Camelize());
            notification[nameof(Notification.Sender).Camelize()] = dict.TryGetValue(userId, out var User) ? User : null!;
        }
    }
}