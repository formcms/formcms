using FormCMS.Comments.Models;
using FormCMS.Infrastructure.RelationDbDao;
using Humanizer;

namespace FormCMS.Comments.Builders;

public static class DatabaseMigratorExtensions
{
    public static async Task EnsureCommentsTable(this IRelationDbDao  migrator)
    {
        await migrator.MigrateTable(CommentHelper.Entity.TableName, CommentHelper.Columns);
    }
}