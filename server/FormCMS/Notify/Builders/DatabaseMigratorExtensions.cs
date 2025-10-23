using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using Humanizer;

namespace FormCMS.Notify.Builders;

public static class DatabaseMigratorExtensions
{
    public static async Task EnsureNotifyTable(this IRelationDbDao  migrator)
    {
        await migrator.MigrateTable(Notifications.TableName, Notifications.Columns);
        await migrator.MigrateTable(NotificationCountExtensions.TableName, NotificationCountExtensions.Columns);
        await migrator.CreateIndex(
            NotificationCountExtensions.TableName,
            [nameof(NotificationCount.UserId).Camelize()],
            true,
            CancellationToken.None
        );   
    }
    
}