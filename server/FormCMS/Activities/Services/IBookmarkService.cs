using FormCMS.Activities.Models;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Activities.Services;

public interface IBookmarkService
{
    Task EnsureBookmarkTables();
    Task<Record[]> Folders(CancellationToken ct);
    Task<Record[]> FolderWithRecordStatus(string entityName, long recordId, CancellationToken ct);
    Task UpdateFolder(long id, BookmarkFolder folder, CancellationToken ct);
    Task DeleteFolder(long folderId, CancellationToken ct);
    
    Task AddBookmark(string entityName, long recordId, string newFolderName, long[] newFolderIds, CancellationToken ct);
    Task<ListResponse> List(long folderId, StrArgs args, int? offset, int? limit, CancellationToken ct);
    Task DeleteBookmark(long bookmarkId, CancellationToken ct);
}