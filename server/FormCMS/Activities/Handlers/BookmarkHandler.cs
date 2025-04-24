using FormCMS.Activities.Models;
using FormCMS.Activities.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Activities.Handlers;

public record SaveBookmarkPayload(string NewFolderName, long[] SelectedFolders);
public static class BookmarkHandler
{
    public static RouteGroupBuilder MapBookmarkHandler(this RouteGroupBuilder builder)
    {
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
        
        builder.MapPost("/folders/update/{id:long}", (
            long id,
            IBookmarkService s,
            BookmarkFolder folder,
            CancellationToken ct
        ) => s.UpdateFolder(id, folder, ct));

        builder.MapPost("/folders/delete/{id:long}", (
            IBookmarkService s,
            long id,
            CancellationToken ct
        ) => s.DeleteFolder(id, ct));

        builder.MapGet("/list/{folderId:long}", (
            CancellationToken ct,
            HttpContext context,
            long folderId,
            int? offset,
            int? limit,
            IBookmarkService s
        ) => s.List(folderId, QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, ct));
        
        builder.MapPost("/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            SaveBookmarkPayload payload,
            IBookmarkService s,
            CancellationToken ct
        ) => s.AddBookmark(entityName, recordId, payload.NewFolderName,payload.SelectedFolders, ct));
        
        builder.MapPost("/delete/{id:long}", (
            IBookmarkService s,
            long id,
            CancellationToken ct
        ) => s.DeleteBookmark(id, ct)); 
        return builder;
    }
}