using FormCMS.Core.Descriptors;

namespace FormCMS.Comments.Services;

public interface ICommentsQueryPlugin
{
    Task<Record[]> GetTags(string[] commentIds);
    Task AttachComments(
        LoadedEntity entity,
        GraphNode[] nodes,
        Record record,
        StrArgs args,
        CancellationToken ct);

    //use case, get comments by parentId
    Task<Record[]> GetByFilters(string entityName, long recordId, ValidFilter[] filters, ValidSort[] sorts, ValidPagination pagination, ValidSpan span,
        CancellationToken ct);

    Task<Record[]> GetByEntityRecordId(string entityName, long recordId,
        ValidPagination pg, ValidSpan sp, ValidSort[] sorts, CancellationToken ct);
}