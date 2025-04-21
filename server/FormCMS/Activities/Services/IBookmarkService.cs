using FormCMS.Activities.Models;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Activities.Services;

public interface IBookmarkService
{
    XEntity GetEntity();
    Task EnsureBookmarkTables();
    Task<Record[]> Folders(CancellationToken ct);
    Task<Record[]> FolderWithRecordStatus(string entityName, long recordId, CancellationToken ct);
    Task<BookmarkFolder> AddFolder(BookmarkFolder folder, CancellationToken ct);
    Task AddBookmark(string entityName, long recordId, string newFolderName, long[] newFolderIds, CancellationToken ct);
}