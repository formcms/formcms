using FormCMS.Cms.Services;
using FormCMS.Comments.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Comments.Services;

public class CommentsQueryPlugin(
    IEntitySchemaService  schemaService,
    IContentTagService  contentTagService,
    CommentsContext ctx
) : ICommentsQueryPlugin
{
    public async Task<Record[]> GetByFilters( string entityName, long recordId, ValidFilter[] filters, ValidSort[] sorts, ValidPagination pagination,
        ValidSpan span,
        CancellationToken ct)
    {
        var kateQuery = CommentHelper.List(filters, sorts, span, pagination);
        var  key = new Comment(entityName,recordId).GetSourceKey();
        var executor = ctx.RecordCommentShardRouter.ReplicaDao(key);
        return await executor.Many(kateQuery, ct);
    }

    // when a user liked a comments, a notification message will call this function
    public async Task<Record[]> GetTags(string[] commentIds)
    {
        var comments = commentIds.Select(CommentHelper.Parse).ToArray();
        var group = comments.GroupBy(x => x.EntityName);
        var ret = new List<Record>();
        foreach (var grouping in group)
        {
            var entity = await schemaService
                .LoadEntity(grouping.Key, PublicationStatus.Published, CancellationToken.None).Ok();

            var ids = grouping.Select(x => x.RecordId.ToString()).ToArray();
            var tags = await contentTagService.GetContentTags(entity,ids,CancellationToken.None);
            var map = tags.ToDictionary(x => x.RecordId);
            foreach (var comment in grouping)
            {
                if (!map.TryGetValue(comment.RecordId.ToString(), out var tag)) continue;
              
                var record = new Dictionary<string, object>
                {
                    [CommentHelper.Entity.PrimaryKey] = comment.Id,
                    [CommentHelper.Entity.TitleTagField] = "on " + tag.Title,
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
    
    public Task<Record[]> GetByEntityRecordId(string entityName, long recordId,
        ValidPagination pg, ValidSpan? sp, ValidSort[] sorts, CancellationToken ct)
    {
        var query = CommentHelper.List(entityName, recordId, sorts, sp, pg);
        return  ctx.RecordCommentShardRouter.ReplicaDao(new Comment(entityName,recordId).GetSourceKey()).Many(query, ct);
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
                var comments = await ctx.RecordCommentShardRouter.ReplicaDao(new Comment(entity.Name,recordId).GetSourceKey()).Many(kateQuery, ct);
                comments = new Span().ToPage(comments, validPagination.Limit);
                rec[node.Field] = comments;
            }
        });
    }
}