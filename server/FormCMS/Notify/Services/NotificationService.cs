using FormCMS.Cms.Services;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Notify.Services;

public class NotificationService(
    DatabaseMigrator migrator,
    IIdentityService  identityService,
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
        var countQuery = Notifications.Count(userId);
        var count = await executor.Count(countQuery,Models.Notifications.Columns,filters,ct);
        return new ListResponse(items,count); 
    }
    
    public  Task<int> UnreadCount(CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User not logged in");
        var query = Notifications.UnreadCount(userId);
        return executor.Count(query,ct);
    }
}