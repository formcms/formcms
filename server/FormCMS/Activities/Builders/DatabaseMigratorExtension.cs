using FormCMS.Activities.Models;
using FormCMS.Infrastructure.RelationDbDao;
using Humanizer;

namespace FormCMS.Activities.Builders;

public static class DatabaseMigratorExtension
{
    public static async Task EnsureBookmarkTables(this IRelationDbDao migrator)
    {
        await migrator.MigrateTable(BookmarkFolders.TableName, BookmarkFolders.Columns);
        await migrator.MigrateTable(Bookmarks.TableName, Bookmarks.Columns);

        await migrator.CreateForeignKey(
            Bookmarks.TableName, nameof(Bookmark.FolderId).Camelize(),
            BookmarkFolders.TableName, nameof(BookmarkFolder.Id).Camelize(),
            CancellationToken.None
        );
    }
    public static async Task EnsureCountTable(this IRelationDbDao dao)
    {
        await dao.MigrateTable(ActivityCounts.TableName, ActivityCounts.Columns);
        await dao.CreateIndex(ActivityCounts.TableName, ActivityCounts.KeyFields, true, CancellationToken.None);
    }
    public static async Task EnsureActivityTable(this IRelationDbDao dao)
    {
        await dao.MigrateTable(Models.Activities.TableName, Models.Activities.Columns);
        await dao.CreateIndex(Models.Activities.TableName, Models.Activities.KeyFields, true, CancellationToken.None);
    } 
}