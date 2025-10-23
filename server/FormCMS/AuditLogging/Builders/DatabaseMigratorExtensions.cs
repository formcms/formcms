using FormCMS.AuditLogging.Models;
using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.AuditLogging.Builders;

public static class DatabaseMigratorExtensions
{
    public static async Task EnsureAuditLogTables(this IRelationDbDao migrator)
    {
        await migrator.MigrateTable(AuditLogConstants.TableName,AuditLogHelper.Columns);
    }
}