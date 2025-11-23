using FormCMS.Comments.Models;

namespace FormCMS.Comments.Services;

public interface ICommentsService
{
    Task<Comment> Add(Comment comment,CancellationToken ct);
    Task Update(Comment comment, CancellationToken ct);
    Task Delete(string id,  CancellationToken ct);
    Task<Comment> Reply(string referencedId, Comment comment, CancellationToken ct);
}