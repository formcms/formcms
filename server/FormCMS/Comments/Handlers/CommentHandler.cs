using FormCMS.Comments.Models;
using FormCMS.Comments.Services;

namespace FormCMS.Comments.Handlers;

public static class CommentHandler
{
    public static RouteGroupBuilder MapCommentHandlers(this RouteGroupBuilder builder)
    {
        builder.MapPost("/", (ICommentsService s, Comment c, CancellationToken ct) => s.Add(c, ct));
        builder.MapPost("/update", (ICommentsService s, Comment c, CancellationToken ct) => s.Update(c, ct));
        builder.MapPost("/delete/{id:long}", (ICommentsService s, long id, CancellationToken ct) => s.Delete(id, ct));
        return builder;
    }
}