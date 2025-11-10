using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Subscriptions.Models;
using Humanizer;

namespace FormCMS.Subscriptions.Builders;

public static class DatabaseMigratorExtensions
{
    public static  async Task EnsureSubscriptionTables(this IPrimaryDao migrator)
    {
        await migrator.MigrateTable(Billings.TableName, Billings.Columns);
        await migrator.CreateIndex(Billings.TableName, [nameof(Billing.UserId).Camelize()], true,
            CancellationToken.None); 
    }
}