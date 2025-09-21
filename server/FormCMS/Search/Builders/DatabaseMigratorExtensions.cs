using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Search.Models;

namespace FormCMS.Search.Builders;

public static class DatabaseMigratorExtensions
{
    public static async Task EnsureFtsTables(this DatabaseMigrator migrator)
    {
        await migrator.MigrateTable(SearchConstants.TableName, SearchDocumentHelper.Columns);
        await migrator.CreateIndex(SearchConstants.TableName, SearchDocumentHelper.UniqKeyFields,true,CancellationToken.None);
    }


    public static async Task EnsureFtsIndex(this IFullTextSearch fts)
    {
        await fts.CreateFtsIndex(SearchConstants.TableName, SearchDocumentHelper.FtsFields,CancellationToken.None);
    }
}