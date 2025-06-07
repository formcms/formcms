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

public class CommentsQueryPlugin(
    IUserManageService userManageService,
    KateQueryExecutor executor
) : ICommentsQueryPlugin
{

    public async Task<Record[]> GetCommentReplies(long parentId, Pagination pagination, Span span, CancellationToken ct)
    {
        var pg = PaginationHelper.ToValid(pagination, null,CommentHelper.MaxCommentCount,!span.IsEmpty(),[]);
        var sp = span.ToValid([..CommentHelper.LoadedEntity.Attributes]).Ok();
        var sorts = new []{new Sort(nameof(Comment.Id).Camelize(), SortOrder.Asc)};
 
        var kateQuery = CommentHelper.List(parentId, sorts,sp, pg.PlusLimitOne());
        var comments = await executor.Many(kateQuery, ct);
        comments = SetSpan(comments,span, sorts,pg.Limit);
        SetRecordId(comments);
        await AttachUserInfo(comments, ct);
        return comments;    
    }
    
    public async Task<Record[]> GetComments(string entityName, long recordId,
        ExtendedGraphAttribute commentsAttr,Span span,CancellationToken ct)
    {
        var pg = PaginationHelper.ToValid(commentsAttr.Pagination, null,CommentHelper.MaxCommentCount,!span.IsEmpty(),[]);
        var sp = span.ToValid([..CommentHelper.LoadedEntity.Attributes]).Ok();
        
        var kateQuery = CommentHelper.List(entityName, recordId, commentsAttr.Sorts, sp, pg.PlusLimitOne());
        var comments = await executor.Many(kateQuery, ct);
        comments = SetSpan(comments,span, commentsAttr.Sorts,pg.Limit);
        SetRecordId(comments);
        await AttachUserInfo(comments, ct);
        return comments;
    }
    
    public async Task AttachComments(
        LoadedQuery query,
        Record record,
        CancellationToken ct)
    {
        var commentsAttr = query.ExtendedSelection.FirstOrDefault(x => x.Field == CommentHelper.CommentsField);
        if (commentsAttr is null) return;

        record[CommentHelper.CommentsField] =
            await GetComments(query.Entity.Name, (long)record[query.Entity.PrimaryKey], commentsAttr,
                new Span(), ct);
    }

    private static void SetRecordId(Record[] records)
    {
        foreach (var record in records)
        {
            record[QueryConstants.RecordId] = record[nameof(Comment.Id).Camelize()];
        }
    }
    private static Record[] SetSpan(Record[] comments, Span span,Sort[] sorts, int limit)
    {
        comments = span.ToPage(comments,limit);
        if (SpanHelper.HasPrevious(comments)) SpanHelper.SetCursor( comments.First(), sorts);
        if (SpanHelper.HasNext(comments)) SpanHelper.SetCursor( comments.Last(), sorts);
        return comments;
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

    private async Task AttachUserInfo(Record[] comments,CancellationToken ct)
    {
        var userIds = GetUserIds(comments);
        var users = await userManageService.GetPublicUserInfos(userIds,ct);
        var userDict = users.ToDictionary(x => x.Id);
        SetUserInfo(comments, userDict);
    }
    
    private static void SetUserInfo(Record[]? comments, Dictionary<string, PublicUserInfo> userInfo)
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