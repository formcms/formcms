using FormCMS.Core.Descriptors;

namespace FormCMS.Comments.Services;

public interface ICommentsQueryPlugin
{
    Task AttachComments(LoadedQuery query, Record records, CancellationToken ct);

    Task<Record[]> GetComments(string entityName, long recordId,
        GraphNode commentsAttr, Span span, CancellationToken ct);

    Task<Record[]> GetCommentReplies(long parentId, Pagination pagination, Span span, CancellationToken ct);
}