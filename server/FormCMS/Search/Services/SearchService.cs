using FormCMS.Core.Descriptors;
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
            offset,limit + 1);
        var records = res.Select(x => x.Document).ToList();
        if (records.Count == 0) return [..records]; 

        if (offset > 0)
        {
            records[0][SpanConstants.HasPreviousPage]= true;
            records[0][SpanConstants.Cursor]= offset;
        }

        if (records.Count <= limit) return records.ToArray();
        
        records.RemoveAt(records.Count - 1);
        var last = records.Last();
        last[SpanConstants.HasNextPage]= true;
        last[SpanConstants.Cursor]= offset + limit -1;
        
        return records.ToArray();
    }
}