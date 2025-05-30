using FormCMS.Cms.Services;
using FormCMS.Comments.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.ResultExt;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Comments.Services;

public class CommentsQueryQueryPlugin(
    KateQueryExecutor executor,
    IIdentityService identityService
) : ICommentsQueryPlugin
{

    public async Task<Record[]> GetPartialQueryComments(LoadedQuery query,ExtendedGraphAttribute commentsAttr,Span span,long recordId, CancellationToken ct)
    {
        var pg = PaginationHelper.ToValid(commentsAttr.Pagination, null,CommentHelper.MaxCommentCount,!span.IsEmpty(),[]);
        var sp = span.ToValid([..CommentHelper.LoadedEntity.Attributes]).Ok();
        
        var kateQuery = CommentHelper.List(query.Entity.Name, recordId, commentsAttr.Sorts, sp, pg.PlusLimitOne());
        var comments = await executor.Many(kateQuery, ct);
        comments = SetSpan(comments,span, commentsAttr.Sorts,pg.Limit);
      
        var userIds = GetUserIds(comments);
        var users = await identityService.GetPublicUserInfos(userIds,ct);
        var userDict = users.ToDictionary(x => x.Id);
        SetUserInfo(comments,userDict);
        return comments;
    }
    
    public async Task LoadComments(
        LoadedQuery query,
        Record[] records,
        CancellationToken ct)
    {
        var commentsAttr = query.ExtendedSelection.FirstOrDefault(x => x.Field == CommentHelper.CommentsField);
        if (commentsAttr is null) return;
        await GetComments(records, query.Entity, commentsAttr.Sorts, commentsAttr.Pagination, ct);
        await LoadPublicUserInfos(records, ct);
    }

    public Entity[] ExtendEntities(IEnumerable<Entity> entities)
    {
        var result = entities.Select(e => e with
        {
            Attributes =
            [
                ..e.Attributes,
                new Attribute(Field: CommentHelper.CommentsField, Header: "Comments", DataType: DataType.Collection,
                    Options: CommentHelper.Entity.Name + "." + nameof(Comment.RecordId).Camelize())
            ]
        }).ToList();

        result.Add(CommentHelper.Entity);
        return result.ToArray();
    }
    
    private static Record[] SetSpan(Record[] comments, Span span,Sort[] sorts, int limit)
    {
        comments = span.ToPage(comments,limit);
        if (SpanHelper.HasPrevious(comments)) SpanHelper.SetCursor( comments.First(), sorts);
        if (SpanHelper.HasNext(comments)) SpanHelper.SetCursor( comments.Last(), sorts);
        return comments;
    }
    

    private async Task GetComments(Record[] records, LoadedEntity entity, Sort[] sorts, Pagination pagination, CancellationToken ct)
    {
        var pg = PaginationHelper.ToValid(pagination, CommentHelper.MaxCommentCount);
        foreach (var record in records)
        {
            var span = new Span();
            var id = record[entity.PrimaryKey];
            var query = CommentHelper.List(entity.Name, (long)id, sorts, new ValidSpan(new Span()),pg.PlusLimitOne());
            var comments = await executor.Many(query, ct);
            
            comments = SetSpan(comments,span, sorts,pg.Limit);
            record[CommentHelper.CommentsField] = comments;
        }
    }
    
    private async Task LoadPublicUserInfos(Record[] records, CancellationToken ct)
    {
        var userIds = new HashSet<string>();
        userIds = records.Aggregate(userIds,
            (current, record) => [..current, ..GetUserIds(record[CommentHelper.CommentsField] as Record[])]);

        var users = await identityService.GetPublicUserInfos(userIds,ct);
        var userDict = users.ToDictionary(x => x.Id);
        foreach (var record in records)
        {
            SetUserInfo(record[CommentHelper.CommentsField] as Record[],userDict);
        }
    }
    
    private HashSet<string> GetUserIds(Record[]? comments)
    {   
        var userIds = new HashSet<string>();
        foreach (var commentRec in comments??[])
        {
            if (!commentRec.TryGetValue(nameof(Comment.User).Camelize(), out var obj) ||
                obj is not string userId) continue;
            userIds.Add(userId);
        }
        return userIds;
    }
    
    private void SetUserInfo(Record[]? comments, Dictionary<string, PublicUserInfo> userInfo)
    {   
        foreach (var commentRec in comments??[])
        {
            if (!commentRec.TryGetValue(nameof(Comment.User).Camelize(), out var obj) ||
                obj is not string userId) continue;
            var userObj = userInfo.TryGetValue(userId, out var user)
                ? user
                : new PublicUserInfo(userId, "Unknown", "");
            commentRec[nameof(Comment.User).Camelize()] = userObj;
        }
    }
}