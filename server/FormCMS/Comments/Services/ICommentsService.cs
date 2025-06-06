using FormCMS.Comments.Models;

namespace FormCMS.Comments.Services;

public interface ICommentsService
{
    Task EnsureTable(); 
    Task<Comment> Add(Comment comment,CancellationToken ct);
    Task Update(Comment comment, CancellationToken ct);
    Task Delete(long id, CancellationToken ct);
    Task<Comment> Reply(long referencedId, Comment comment, CancellationToken ct);
}