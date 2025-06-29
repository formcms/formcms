using FormCMS.Cms.Services;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Notify.Services;

public class NotificationService(
    DatabaseMigrator migrator,
    IIdentityService  identityService,
    IUserManageService userManageService,
    KateQueryExecutor executor

    ):INotificationService
{
    public async Task EnsureNotificationTables()
    {
        await migrator.MigrateTable(Notifications.TableName, Notifications.Columns);
    }

    public async Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = Notifications.List(userId, offset, limit);
        var items = await executor.Many(query, Notifications.Columns,filters,sorts,ct);
        await LoadSender(items, ct);
        
        var countQuery = Notifications.Count(userId);
        var count = await executor.Count(countQuery,Notifications.Columns,filters,ct);
        
        await executor.Exec(Notifications.ReadAll(userId), false,ct);
        return new ListResponse(items,count); 
    }
    
    //todo: need another table to save count, to improve performance
    public  Task<int> UnreadCount(CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User not logged in");
        var query = Notifications.UnreadCount(userId);
        return executor.Count(query,ct);
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
            notification[nameof(Notification.Sender).Camelize()] = dict.TryGetValue(userId, out var User) ? User : null;
        }
    }
}