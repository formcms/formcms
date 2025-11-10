using FormCMS.Comments.Models;
using FormCMS.Infrastructure.RelationDbDao;
using Humanizer;

namespace FormCMS.Comments.Builders;

public static class DatabaseMigratorExtensions
{
    public static Task EnsureCommentsTable(this IPrimaryDao migrator)
        => migrator.MigrateTable(CommentHelper.Entity.TableName, CommentHelper.Columns);
}