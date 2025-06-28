using FormCMS.Core.Descriptors;

namespace FormCMS.Comments.Services;

public interface ICommentsQueryPlugin
{
    Task<Record[]> GetTags(long[] commentIds);
    Task AttachComments(
        LoadedEntity entity,
        GraphNode[] nodes,
        Record record,
        StrArgs args,
        CancellationToken ct);

    Task<Record[]> GetByFilters(ValidFilter[] filters, ValidSort[] sorts, ValidPagination pagination, ValidSpan span,
        CancellationToken ct);

    Task<Record[]> GetByEntityRecordId(string entityName, long recordId,
        ValidPagination pg, ValidSpan sp, ValidSort[] sorts, CancellationToken ct);
}