using FormCMS.Cms.Services;
using FormCMS.Comments.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Comments.Services;

public class CommentsService(
    DatabaseMigrator migrator,
    IIdentityService identityService,
    KateQueryExecutor executor
    ):ICommentsService
{
    public async Task EnsureTable()
    {
        await migrator.MigrateTable(CommentHelper.Entity.TableName, CommentHelper.Columns);
    }

    public async Task Delete(long id, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        var commentRec = await executor.Single(CommentHelper.Single(id),ct);
        if (commentRec is null) throw new ResultException("Comment not found");

        var comment = commentRec.ToObject<Comment>().Ok();
        if (userId != comment.User) throw new ResultException("You don't have permission to delete this comment");
        
        await executor.Exec(CommentHelper.Delete(userId, id), false, ct);
    }
    
    public async Task Update(Comment comment, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        comment = comment with { User = userId};
            
        var affected = await executor.Exec(comment.Update(),false, ct);
        if (affected == 0) throw new ResultException("Failed to update comment.");
    } 
    
    public async Task<Comment> Add(Comment comment, CancellationToken ct)
    {
        var user = identityService.GetUserAccess() ?? throw new ResultException("User is not logged in.");
        comment = comment with { User = user.Id };
        var query = comment.Insert();
        var id = await executor.Exec(query, true, ct);
        return comment with { Id = id };
    } 
}