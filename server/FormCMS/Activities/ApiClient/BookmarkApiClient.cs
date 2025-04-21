using FluentResults;
using FormCMS.Activities.Handlers;
using FormCMS.Activities.Models;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Activities.ApiClient;

public class BookmarkApiClient(HttpClient client)
{
    public Task<Result<BookmarkFolder[]>> AllFolders()
        => client.GetResult<BookmarkFolder[]>($"{client.BaseAddress}api/bookmarks/folders/");

    public Task<Result<BookmarkFolder[]>> FolderWithRecordStatus(string entityName, long recordId)
        => client.GetResult<BookmarkFolder[]>($"{client.BaseAddress}api/bookmarks/folders/{entityName}/{recordId}");
    
    public Task<Result<BookmarkFolder>> AddFolders(object payload)
        => client.PostResult<BookmarkFolder>($"{client.BaseAddress}api/bookmarks/folders/", payload);
    
    public Task<Result> AddBookmarks(string entityName, long recordId,string newFolderName, long[] folderIds)
       => client.PostResult(
           $"{client.BaseAddress}api/bookmarks/{entityName}/{recordId}", 
           new SaveBookmarkPayload(newFolderName,folderIds));
}