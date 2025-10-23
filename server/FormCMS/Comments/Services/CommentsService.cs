using FormCMS.Cms.Services;
using FormCMS.Comments.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Messaging;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Comments.Services;

public class CommentsService(
    CommentsContext ctx,
    IEntitySchemaService entityService,
    IContentTagService contentTagService,
    IUserManageService userManageService,
    IIdentityService identityService,
        
    IStringMessageProducer producer
    ):ICommentsService
{
    

    public async Task<Comment> Add(Comment comment, CancellationToken ct)
    {
        var entity = await entityService.ValidateEntity(comment.EntityName,  ct).Ok();
        comment = AssignUser(comment.AssignId());
        var query = comment.Insert();
        await ctx.ShardRouter.PrimaryDao(comment.GetSourceKey()).Exec(query, true, ct);
        var creatorId = await userManageService.GetCreatorId(entity.TableName, entity.PrimaryKey, comment.RecordId, ct);
        var activityMessage = new ActivityMessage(comment.CreatedBy, creatorId, comment.EntityName,
            comment.RecordId.ToString(), CommentHelper.CommentActivity, CmsOperations.Create, comment.Content);
        activityMessage = await SetLinkUrl(activityMessage,entity,comment.RecordId,ct);
        await producer.Produce(CmsTopics.CmsActivity, activityMessage.ToJson());
        return comment;
    }
    
    public async Task Delete(string id, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        var executor = ctx.ShardRouter.PrimaryDao(CommentHelper.Parse(id).GetSourceKey());
        var commentRec = await executor.Single(CommentHelper.Single(id),ct);
        if (commentRec is null) throw new ResultException("Comment not found");
        var comment = commentRec.ToObject<Comment>().Ok();

        if (userId != comment.CreatedBy) throw new ResultException("You don't have permission to delete this comment");
        await executor.Exec(CommentHelper.Delete(userId, id), false, ct);

        if (comment.Parent is not null)
        {
            //while replying, send notification to original comments author
            var parentRecord = await executor.Single(CommentHelper.Single(comment.Parent), ct) ??
                               throw new ResultException("Parent comment not found");
            var parentComment = parentRecord.ToObject<Comment>().Ok();

            var activityMessage = new ActivityMessage(
                userId,
                parentComment.CreatedBy,
                CommentHelper.Entity.Name,
                comment.Parent,
                CommentHelper.CommentActivity,
                CmsOperations.Delete,
                comment.Content);

            await producer.Produce(CmsTopics.CmsActivity, activityMessage.ToJson());
        }
        else
        {
            //new comment, send notification to original post author
            var entity = await entityService.ValidateEntity(comment.EntityName,  ct).Ok();
            var creatorId =  await userManageService.GetCreatorId(entity.TableName,entity.PrimaryKey, comment.RecordId, ct);
            
            var activityMessage = new ActivityMessage(
                userId, creatorId, 
                comment.EntityName, 
                comment.RecordId.ToString() , 
                CommentHelper.CommentActivity, 
                CmsOperations.Delete,comment.Content);
            await producer.Produce(CmsTopics.CmsActivity, activityMessage.ToJson());
        }
    }

    public async Task<Comment> Reply(string referencedId,Comment comment, CancellationToken ct)
    {
        var executor = ctx.ShardRouter.PrimaryDao(comment.GetSourceKey());
        comment = AssignUser(comment.AssignId());
        var parentRecord = await executor.Single(CommentHelper.Single(referencedId), ct) ??
                           throw new ResultException("Parent comment not found");
        var parentComment = parentRecord.ToObject<Comment>().Ok();
        var entity = await entityService.ValidateEntity(comment.EntityName,  ct).Ok();
        comment = comment with
        {
            EntityName = parentComment.EntityName,
            RecordId = parentComment.RecordId,
            Parent = parentComment.Parent ?? parentComment.Id,
            Mention = parentComment.Parent is null ? null :parentComment.CreatedBy
        };
        await executor.Exec(comment.Insert(),true, ct);
        
        var activityMessage = new ActivityMessage(comment.CreatedBy, parentComment.CreatedBy, CommentHelper.Entity.Name,
            parentComment.Id, CommentHelper.CommentActivity, CmsOperations.Create,comment.Content);
        
        activityMessage =await SetLinkUrl(activityMessage,entity,comment.RecordId,ct);
        await producer.Produce(CmsTopics.CmsActivity, activityMessage.ToJson());
        return comment;
    }
    
    public async Task Update(Comment comment, CancellationToken ct)
    {
        comment = AssignUser(comment);
        var executor = ctx.ShardRouter.PrimaryDao(comment.GetSourceKey());
        var affected = await executor.Exec(comment.Update(),false, ct);
        if (affected == 0) throw new ResultException("Failed to update comment.");
    } 

    private Comment AssignUser(Comment comment)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        comment = comment with { CreatedBy = userId};
        return comment;
    }

    
    private async Task<ActivityMessage> SetLinkUrl(ActivityMessage activityMessage,LoadedEntity entity, long recordId, CancellationToken ct)
    {
        var links =await contentTagService.GetContentTags(entity,[recordId.ToString()],ct);
        if (links.Length == 1)
        {
            activityMessage = activityMessage with{Url =links[0].Url};
        }
        return activityMessage;
    }
}