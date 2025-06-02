using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;

namespace FormCMS.Comments.Services;

public interface ICommentsQueryPlugin
{
    Task LoadComments(LoadedQuery query, Record[] records, CancellationToken ct);
    Task<Record[]> GetPartialQueryComments(LoadedQuery query,ExtendedGraphAttribute commentsAttr ,Span span, long recordId, CancellationToken ct);
}