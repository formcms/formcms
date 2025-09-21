using FormCMS.Comments.Models;
using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Comments.Builders;

public static class DatabaseMigratorExtensions
{
    public static async Task EnsureCommentsTable(this DatabaseMigrator  migrator)
    {
        await migrator.MigrateTable(CommentHelper.Entity.TableName, CommentHelper.Columns);
    }
}