using FormCMS.Cms.Services;
using FormCMS.Comments.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Comments.Services;

public class CommentsQueryPlugin(
    IEntitySchemaService  schemaService,
    KateQueryExecutor executor
) : ICommentsQueryPlugin
{
    public async Task<Record[]> GetByFilters(ValidFilter[] filters,ValidSort[] sorts, ValidPagination pagination, ValidSpan span,
        CancellationToken ct)
    {
        var kateQuery = CommentHelper.List(filters, sorts,span,pagination);
        return await executor.Many(kateQuery, ct);
    }
    
    public Task<Record[]> GetByEntityRecordId(string entityName, long recordId,
        ValidPagination pg, ValidSpan? sp, ValidSort[] sorts, CancellationToken ct)
    {
        var kateQuery = CommentHelper.List(entityName, recordId, sorts, sp, pg);
        return  executor.Many(kateQuery, ct);
    }
    
    public Task AttachComments(
        LoadedEntity entity,
        GraphNode[] nodes,
        Record record,
        StrArgs args,
        CancellationToken ct)
    {
        return nodes.IterateAsync(entity,[record], async (entity, node, rec) =>
        {
            if (node.Field == CommentHelper.CommentsField)
            {
                var sorts = await SortHelper.ReplaceVariables(node.ValidSorts, args, entity, schemaService,
                    PublicationStatusHelper.GetSchemaStatus(args)).Ok();
                var recordId = (long)record[entity.PrimaryKey];
                var variablePagination = PaginationHelper.FromVariables(args, node.Prefix, node.Field);
                var validPagination = PaginationHelper.MergePagination(variablePagination, node.Pagination,args, CommentHelper.DefaultPageSize );
                var kateQuery = CommentHelper.List(entity.Name, recordId, sorts,null ,validPagination.PlusLimitOne());
                var comments = await executor.Many(kateQuery, ct);
                comments = new Span().ToPage(comments, validPagination.Limit);
                rec[node.Field] = comments;
            }
        });
    }
}