using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Engagements.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Engagements.Services;

public class BookmarkService(
    IIdentityService identityService,
    IEntitySchemaService entityService,
    IContentTagService contentTagService,
    EngagementContext ctx
) : IBookmarkService
{
    public Task<Record[]> Folders(CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        return GetFoldersByUserId(userId, ct);
    }

    public async Task<Record[]> FolderWithRecordStatus(string entityName, long recordId, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
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
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        folder = folder with { UserId = userId, Id=id};
            
        var affected = await ctx.UserActivityShardRouter.PrimaryDao(userId).Exec(folder.Update(),ct);
        if (affected == 0) throw new ResultException("Failed to update folder.");
    }

    public async Task DeleteFolder(long folderId, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        
        var dao = ctx.UserActivityShardRouter.PrimaryDao(userId);
        var executor = ctx.UserActivityShardRouter.PrimaryDao(userId);

        using var trans = await dao.BeginTransaction();
        try
        {
            await executor.Exec(Bookmarks.DeleteBookmarksByFolder(userId, folderId),ct);
            await executor.Exec(BookmarkFolders.Delete(userId, folderId), ct);
            trans.Commit();
        }
        catch 
        {
            trans.Rollback();
            throw;
        }
    }
    
    public async Task<ListResponse> List(long folderId, StrArgs args, int?offset, int?limit, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in");
        var (filters, sorts) = QueryStringParser.Parse(args); 
        var executor = ctx.UserActivityShardRouter.ReplicaDao(userId);
        var listQuery = Bookmarks.List(userId, folderId, offset, limit);
        var items = await executor.Many(listQuery, Models.Bookmarks.Columns,filters,sorts,ct);
        var countQuery = Bookmarks.Count(userId, folderId);
        var count = await executor.Count(countQuery,Models.EngagementStatusHelper.Columns,filters,ct);
        return new ListResponse(items,count);  
    }

    //folderId 0, means default folder, to avoid foreign key error, need to convert it to null
    public async Task AddBookmark(string entityName, long recordId, string newFolderName, long[] newFolderIds,
        CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        var entity = await entityService.ValidateEntity(entityName, ct).Ok();
        var existingFolderIds = await GetFolderIdsByUserAndRecord(userId, entityName, recordId, ct);

        var toAdd = newFolderIds.Except(existingFolderIds).ToArray();
        var toDelete = existingFolderIds.Except(newFolderIds).ToArray();

        var executor = ctx.UserActivityShardRouter.PrimaryDao(userId);

        foreach (var l in toDelete)
        {
            var q = Bookmarks.Delete(userId, entityName, recordId, l > 0 ? l : null);
            await executor.Exec(q, ct);
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

        var count = new Models.EngagementCount(entityName, recordId.ToString(), Bookmarks.ActivityType, 1);
        await ctx.UserActivityShardRouter.PrimaryDao(userId).Increase(
            EngagementCountHelper.TableName, 
            EngagementCountHelper.Condition(count.EntityName, count.RecordId, count.EngagementType),
            EngagementCountHelper.CountField, 
            0,
            1, 
            ct);
    }

    public Task DeleteBookmark(long bookmarkId, CancellationToken ct)
    {
        var userId = identityService.GetUserAccess()?.Id ?? throw new ResultException("User is not logged in.");
        var query = Bookmarks.Delete(userId, bookmarkId);
        return ctx.UserActivityShardRouter.PrimaryDao(userId).Exec(query, ct);
    }

    private async Task<Bookmark[]> LoadMetaData(LoadedEntity entity, Bookmark[] bookmarks, CancellationToken ct)
    {
        var ids = bookmarks
            .Select(x => x.RecordId.ToString())
            .ToArray();
        if (ids.Length == 0) return bookmarks;

        var links = await contentTagService.GetContentTags(entity , ids, ct);
        
        var dict = links.ToDictionary(x => x.RecordId);
        return bookmarks.Select(activity=>
        {
            if (dict.TryGetValue(activity.RecordId.ToString(), out var link))
            {
                return activity with
                {
                    Title = link.Title,
                    Url = link.Url,
                    Image = link.Image,
                    Subtitle = link.Subtitle,
                    PublishedAt = link.PublishedAt 
                };
            }
            return activity;

        }).ToArray();
    }
    
    private async Task<BookmarkFolder> AddFolder(string userId,BookmarkFolder folder, CancellationToken ct)
    {
         folder = folder with { UserId = userId };
         var query = folder.Insert();
         var id = await ctx.UserActivityShardRouter.PrimaryDao(userId).ExecuteScalar(query, ct);
         folder = folder with { Id = id };
         return folder;       
    }

    private async Task<Record[]> GetFoldersByUserId(string userId, CancellationToken ct)
    {
        var records = await ctx.UserActivityShardRouter.ReplicaDao(userId).Many(BookmarkFolders.All(userId), ct);
        records = [new BookmarkFolder("", "", "", Id: 0).ToRecord(), ..records];
        return records;
    }

    private async Task<long[]> GetFolderIdsByUserAndRecord(string userId, string entityName, long recordId,
        CancellationToken ct)
    {
        var getExistingQuery = Bookmarks.FolderIdByUserIdRecordId(userId, entityName, recordId);
        var existing = await ctx.UserActivityShardRouter.ReplicaDao(userId).Many(getExistingQuery, ct);
        return existing.Select(x =>
        {
            var val = x[nameof(Bookmark.FolderId).Camelize()];
            return val is null ? 0 : (long)val;
        }).ToArray();
    }
}