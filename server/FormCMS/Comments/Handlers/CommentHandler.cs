using FormCMS.Comments.Models;
using FormCMS.Comments.Services;

namespace FormCMS.Comments.Handlers;

public static class CommentHandler
{
    public static RouteGroupBuilder MapCommentHandlers(this RouteGroupBuilder builder)
    {
        builder.MapPost("/",
            (
                ICommentsService s,
                Comment c,
                CancellationToken ct
            ) => s.Add(c, ct)
        );
        
        builder.MapPost("/update",
            (
                ICommentsService s,
                Comment c,
                CancellationToken ct
            ) => s.Update(c, ct)
        );
        
        builder.MapPost("/reply/{referencedId}",
            (
                ICommentsService s, 
                Comment c, 
                string referencedId, 
                CancellationToken ct
            ) => s.Reply(referencedId, c, ct)
        );
        
        builder.MapPost("/delete/{id}",
            (
                ICommentsService s,
                string id,
                CancellationToken ct
            ) => s.Delete(id, ct)
        );
        
        return builder;
    }
}