using FormCMS.Infrastructure.Fts;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Search.Models;

namespace FormCMS.Search.Builders;

public static class DatabaseMigratorExtensions
{
    public static async Task EnsureFtsTables(this IRelationDbDao dao)
    {
        await dao.MigrateTable(SearchConstants.TableName, SearchDocumentHelper.Columns);
        await dao.CreateIndex(SearchConstants.TableName, SearchDocumentHelper.UniqKeyFields,true,CancellationToken.None);
    }


    public static async Task EnsureFtsIndex(this IFullTextSearch fts)
    {
        await fts.CreateFtsIndex(SearchConstants.TableName, SearchDocumentHelper.FtsFields,CancellationToken.None);
    }
}