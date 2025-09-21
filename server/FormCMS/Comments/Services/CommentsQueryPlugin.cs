using FormCMS.Cms.Services;
using FormCMS.Comments.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Comments.Services;

public class CommentsQueryPlugin(
    IEntitySchemaService  schemaService,
    IContentTagService  contentTagService,
    KateQueryExecutor executor
) : ICommentsQueryPlugin
{
    public async Task<Record[]> GetTags(long[] commentIds)
    {
        var query = CommentHelper.Multiple(commentIds);
        var commentRecords = await executor.Many(query);
        if (commentRecords.Length == 0) return commentRecords;
        var ret = new List<Record>();

        var group = commentRecords.GroupBy(x => (string)x[nameof(Comment.EntityName).Camelize()]);
        foreach (var grouping in group)
        {
            var entity = await schemaService
                .LoadEntity(grouping.Key, PublicationStatus.Published, CancellationToken.None).Ok();

            var ids = grouping.Select(x => x.StrOrEmpty(nameof(Comment.RecordId).Camelize())).ToArray();
            var tags = await contentTagService.GetContentTags(entity,ids,CancellationToken.None);
            var map = tags.ToDictionary(x => x.RecordId);
            foreach (var rec in grouping)
            {
                var recordId = rec.StrOrEmpty(nameof(Comment.RecordId).Camelize());
                if (!map.TryGetValue(recordId, out var tag)) continue;
              
                var record = new Dictionary<string, object>
                {
                    [CommentHelper.Entity.PrimaryKey] = rec.StrOrEmpty(nameof(Comment.Id).Camelize()),
                    [CommentHelper.Entity.TitleTagField] = rec.StrOrEmpty(nameof(Comment.Content).Camelize()),
                    [CommentHelper.Entity.ImageTagField] = tag.Image,
                    [CommentHelper.Entity.PageUrl] = tag.Url+"?comment_id=",
                    [CommentHelper.Entity.SubtitleTagField] = tag.Subtitle,
                };

                if (tag.PublishedAt is not null)
                {
                    record[CommentHelper.Entity.PublishTimeTagField] = tag.PublishedAt;
                }
                
                ret.Add(record);
            }
        }
        return ret.ToArray();
    }
    
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