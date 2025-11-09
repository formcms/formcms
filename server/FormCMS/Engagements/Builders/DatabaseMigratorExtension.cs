using FormCMS.Engagements.Models;
using FormCMS.Infrastructure.RelationDbDao;
using Humanizer;

namespace FormCMS.Engagements.Builders;

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
        await dao.MigrateTable(EngagementCountHelper.TableName, EngagementCountHelper.Columns);
        await dao.CreateIndex(EngagementCountHelper.TableName, EngagementCountHelper.KeyFields, true, CancellationToken.None);
    }
    public static async Task EnsureEngagementStatusTable(this IRelationDbDao dao)
    {
        await dao.MigrateTable(EngagementStatusHelper.TableName, EngagementStatusHelper.Columns);
        await dao.CreateIndex(EngagementStatusHelper.TableName, EngagementStatusHelper.KeyFields, true, CancellationToken.None);
    } 
}