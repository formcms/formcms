using FormCMS.Activities.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.Cache;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Activities.Services;

public class BookmarkService(
    IProfileService profileService,
    IQueryService queryService,
    IEntitySchemaService schemaService,
    KeyValueCache<long> maxRecordIdCache,

    DatabaseMigrator migrator,
    IRelationDbDao dao,
    KateQueryExecutor executor
) : IBookmarkService
{
    public async Task EnsureBookmarkTables()
    {
        await migrator.MigrateTable(BookmarkFolders.TableName, BookmarkFolders.Columns);
        await migrator.MigrateTable(Bookmarks.TableName, Bookmarks.Columns);

        await dao.CreateForeignKey(
            Bookmarks.TableName, nameof(Bookmark.FolderId).Camelize(),
            BookmarkFolders.TableName, nameof(BookmarkFolder.Id).Camelize(),
            CancellationToken.None
        );
    }

    public Task<Record[]> Folders(CancellationToken ct)
    {
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in.");
        return GetFoldersByUserId(userId, ct);
    }

    public async Task<Record[]> FolderWithRecordStatus(string entityName, long recordId, CancellationToken ct)
    {
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in.");
        var folders = await GetFoldersByUserId(userId, ct);
        var existingFolderIds = await GetFolderIdsByUserAndRecord(userId, entityName, recordId, ct);
        foreach (var folder in folders)
        {
            var id = (long)folder[nameof(BookmarkFolder.Id).Camelize()];
            folder[nameof(BookmarkFolder.Selected).Camelize()] = existingFolderIds.Contains(id);
        }

        return folders;
    }

    public async Task UpdateFolder(long id,BookmarkFolder folder, CancellationToken ct)
    {
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in.");
        folder = folder with { UserId = userId, Id=id};
            
        var affected = await executor.Exec(folder.Update(),false, ct);
        if (affected == 0) throw new ResultException("Failed to update folder.");
    }

    public async Task DeleteFolder(long folderId, CancellationToken ct)
    {
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in.");
        using var trans = await dao.BeginTransaction();
        try
        {
            await executor.Exec(Bookmarks.DeleteBookmarksByFolder(userId, folderId),false,ct);
            await executor.Exec(BookmarkFolders.Delete(userId, folderId), false, ct);
            trans.Commit();
        }
        catch (Exception e)
        {
            trans.Rollback();
            throw;
        }
    }
    
    public async Task<ListResponse> List(long folderId, StrArgs args, int?offset, int?limit, CancellationToken ct)
    {
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in");
        var (filters, sorts) = QueryStringParser.Parse(args); 
        var listQuery = Bookmarks.List(userId, folderId, offset, limit);
        var items = await executor.Many(listQuery, Models.Bookmarks.Columns,filters,sorts,ct);
        var countQuery = Bookmarks.Count(userId, folderId);
        var count = await executor.Count(countQuery,Models.Activities.Columns,filters,ct);
        return new ListResponse(items,count);  
    }

    //folderId 0, means default folder, to avoid foreign key error, need to convert it to null
    public async Task AddBookmark(string entityName, long recordId, string newFolderName, long[] newFolderIds,
        CancellationToken ct)
    {
        Console.Write($"""
                      adding bookmark {entityName} to {newFolderName}""
                      """);
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in.");
        var entity =
            await Utils.EnsureEntityRecordExists(schemaService, dao, maxRecordIdCache, entityName, recordId, ct);
        var existingFolderIds = await GetFolderIdsByUserAndRecord(userId, entityName, recordId, ct);

        var toAdd = newFolderIds.Except(existingFolderIds).ToArray();
        var toDelete = existingFolderIds.Except(newFolderIds).ToArray();

        foreach (var l in toDelete)
        {
            var q = Bookmarks.Delete(userId, entityName, recordId, l > 0 ? l : null);
            await executor.Exec(q, false, ct);
        }

        if (!string.IsNullOrWhiteSpace(newFolderName))
        {
            var newFolder = await AddFolder(userId, new BookmarkFolder("",newFolderName,""), ct);
            toAdd = [..toAdd, newFolder.Id];
        }

        if (toAdd.Length > 0)
        {
            var bookmarks = toAdd
                .Select(x => new Bookmark(entityName, recordId, userId, x > 0 ? x : null))
                .ToArray();
            var loadedBookmark = await LoadMetaData(entity, bookmarks, ct);
            var records = loadedBookmark.Select(x => x.ToInsertRecord()).ToArray();
            await executor.BatchInsert(Bookmarks.TableName,records);
        }

        var count = new ActivityCount(entityName, recordId, Bookmarks.ActivityType, 1);
        await dao.Increase(
            ActivityCounts.TableName, count.Condition(true),
            ActivityCounts.CountField, 1, ct);
    }

    public Task DeleteBookmark(long bookmarkId, CancellationToken ct)
    {
        var userId = profileService.GetInfo()?.Id ?? throw new ResultException("User is not logged in.");
        return executor.Exec(Bookmarks.Delete(userId, bookmarkId), false, ct);
    }

    private async Task<Bookmark[]> LoadMetaData(Entity entity, Bookmark[] bookmarks, CancellationToken ct)
    {
        var ids = bookmarks
            .Select(x => x.RecordId.ToString())
            .ToArray();
        if (ids.Length == 0) return bookmarks;

        var strAgs = new StrArgs
        {
            [entity.BookmarkQueryParamName] = ids
        };
        var records = await queryService.ListWithAction(entity.BookmarkQuery, new Span(), new Pagination(), strAgs, ct);
        var dict = records.ToDictionary(x => x[entity.PrimaryKey].ToString());

        var list = new List<Bookmark>();
        foreach (var ac in bookmarks)
        {
            if (dict.TryGetValue(ac.RecordId.ToString(), out var record))
            {
                list.Add(ac.LoadMetaData(entity, record));
            }
        }

        return list.ToArray();
    }
    
    private async Task<BookmarkFolder> AddFolder(string userId,BookmarkFolder folder, CancellationToken ct)
    {
         folder = folder with { UserId = userId };
         var query = folder.Insert();
         var id = await executor.Exec(query, true, ct);
         folder = folder with { Id = id };
         return folder;       
    }

    private async Task<Record[]> GetFoldersByUserId(string userId, CancellationToken ct)
    {
        var records = await executor.Many(BookmarkFolders.All(userId), ct);
        records = [new BookmarkFolder("", "", "", Id: 0).ToRecord(), ..records];
        return records;
    }

    private async Task<long[]> GetFolderIdsByUserAndRecord(string userId, string entityName, long recordId,
        CancellationToken ct)
    {
        var getExistingQuery = Bookmarks.FolderIdByUserIdRecordId(userId, entityName, recordId);
        var existing = await executor.Many(getExistingQuery, ct);
        return existing.Select(x =>
        {
            var val = x[nameof(Bookmark.FolderId).Camelize()];
            return val is null ? 0 : (long)val;
        }).ToArray();
    }
}