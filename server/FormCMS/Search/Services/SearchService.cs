using FormCMS.Infrastructure.Fts;
using FormCMS.Search.Models;
using Humanizer;

namespace FormCMS.Search.Services;

public class SearchService( IFullTextSearch fts ):ISearchService
{
    private static FtsField[] FtsFields (string query)=> [
        new (nameof(SearchDocument.Title).Camelize(),query,3),
        new (nameof(SearchDocument.Subtitle).Camelize(),query,2),
        new (nameof(SearchDocument.Content).Camelize(),query,1),
    ];

    public async Task<Record[]> Search(string query, int offset, int limit)
    {
        var res = await fts.SearchAsync(
            SearchConstants.TableName,
            FtsFields(query),
            null, null, 
            SearchDocumentHelper.SelectFields,
            offset,limit);
        return res.Select(x => x.Document).ToArray();
    }
}