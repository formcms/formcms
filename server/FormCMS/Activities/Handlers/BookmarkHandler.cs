using FormCMS.Activities.Models;
using FormCMS.Activities.Services;

namespace FormCMS.Activities.Handlers;

public record SaveBookmarkPayload(string NewFolderName, long[] SelectedFolders);
public static class BookmarkHandler
{
    public static RouteGroupBuilder MapBookmarkHandler(this RouteGroupBuilder builder)
    {
        builder.MapGet("/entity", (IBookmarkService s) => s.GetEntity());
        
        builder.MapGet("/folders", (
            IBookmarkService s,
            CancellationToken ct
        ) => s.Folders(ct));

        builder.MapGet("/folders/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            IBookmarkService s,
            CancellationToken ct
        ) => s.FolderWithRecordStatus(entityName, recordId, ct));
        
        builder.MapPost("/folders", (
            IBookmarkService s,
            BookmarkFolder folder,
            CancellationToken ct
        ) => s.AddFolder(folder, ct));

        builder.MapPost("/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            SaveBookmarkPayload payload,
            IBookmarkService s,
            CancellationToken ct
        ) => s.AddBookmark(entityName, recordId, payload.NewFolderName,payload.SelectedFolders, ct));
         
        return builder;
    }
}