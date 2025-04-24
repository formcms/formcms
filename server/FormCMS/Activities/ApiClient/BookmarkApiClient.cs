using System.Text.Json;
using FluentResults;
using FormCMS.Activities.Handlers;
using FormCMS.Activities.Models;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Activities.ApiClient;

public class BookmarkApiClient(HttpClient client)
{
    public Task<Result<JsonElement[]>> AllFolders()
        => client.GetResult<JsonElement[]>( "/folders".BookmarkUrl());

    public Task<Result<BookmarkFolder[]>> FolderWithRecordStatus(string entityName, long recordId)
        => client.GetResult<BookmarkFolder[]>( $"/folders/{entityName}/{recordId}".BookmarkUrl());
    
    public Task<Result> UpdateFolder(long id,BookmarkFolder bookmarkFolder)
        => client.PostResult( $"/folders/update/{id}".BookmarkUrl(), bookmarkFolder);
    
    public Task<Result> DeleteFolder(long id)
        => client.PostResult( $"/folders/delete/{id}".BookmarkUrl(), new{});

    public Task<Result<ListResponse>> ListBookmarks(long folderId)
        => client.GetResult<ListResponse>( $"/list/{folderId}".BookmarkUrl());
    
    
    public Task<Result> AddBookmarks(string entityName, long recordId,string newFolderName, long[] folderIds)
       => client.PostResult(
           $"/{entityName}/{recordId}".BookmarkUrl(), 
           new SaveBookmarkPayload(newFolderName,folderIds));

    public Task<Result> DeleteBookmark(long id)
       => client.PostResult(
           $"/delete/{id}".BookmarkUrl(), new {});
    
}