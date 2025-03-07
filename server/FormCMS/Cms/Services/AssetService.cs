using FormCMS.Core.Files;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.ImageUtil;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using NUlid;

namespace FormCMS.Cms.Services;

public class AssetService(
    IFileStore store,
    DatabaseMigrator migrator,
    KateQueryExecutor executor,
    IProfileService profileService,
    SystemSettings systemSettings  ,
    IRelationDbDao dao,
    IResizer resizer
    ):IAssetService
{
    public async Task<Asset> Single(long id, CancellationToken ct = default)
    {
        var record = await executor.Single(Assets.Single(id), ct);
        return record?.ToObject<Asset>().Ok() ?? throw new ResultException("Asset not found");
    }

    public string GetBaseUrl() => systemSettings.AssetUrlPrefix;
    
    public async Task EnsureTable()
    {
        await migrator.MigrateTable(Assets.TableName, Assets.Columns);
        await migrator.MigrateTable(AssetLinks.TableName, AssetLinks.Columns);
        await dao.CreateForeignKey(
            AssetLinks.TableName, nameof(AssetLink.AssetId).Camelize(), 
            Assets.TableName, nameof(Asset.Id).Camelize(),
            CancellationToken.None);

    }

    public async Task<ListResponse> List(StrArgs args,int? offset, int? limit, bool countLinks, CancellationToken ct)
    {
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = Assets.List(offset, limit);
        var items = await executor.Many(query, Assets.Columns,filters,sorts,ct);
        if (countLinks) await LoadLinkCount(items, ct);
        var count = await executor.Count(Assets.Count(),Assets.Columns,filters,ct);
        return new ListResponse(items,count);
    }

    private async Task LoadLinkCount(Record[] items, CancellationToken ct)
    {
        var ids = items.Select(x => (long)x[nameof(Asset.Id).Camelize()]);
        var dict = await executor.LoadDict(
            AssetLinks.CountByAssetId(ids), 
            nameof(AssetLink.AssetId).Camelize(),
            nameof(Asset.LinkCount).Camelize(), ct);
        foreach (var item in items)
        {
            var id = item.StrOrEmpty(nameof(Asset.Id).Camelize());
            if (dict.TryGetValue(id, out var val) )
            {
                item[nameof(Asset.LinkCount).Camelize()] = val;
            }
        }
    }

    public Task Delete(long id, CancellationToken ct)
        =>executor.Exec(Assets.Deleted(id), false, ct);

    public async Task Replace(string path,IFormFile file)
    {
        if (file.Length == 0) throw new ResultException($"File [{file.FileName}] is empty");
        file = resizer.CompressImage(file);
        await store.UploadAndDispose([(path, file)]);
    }
    
    public async Task<string[]> Add(IFormFile[] files)
    {
        var userInfo = profileService.GetInfo() ?? throw new ResultException("Not logged in");
        foreach (var formFile in files)
        {
            if (formFile.Length == 0) throw new ResultException($"File [{formFile.FileName}] is empty");
        }
        
        files = files.Select(resizer.CompressImage).ToArray();
        var dir = DateTime.Now.ToString("yyyy-MM");
        var pairs = files.Select(x => (Path.Join(dir,GetUniqName(x.FileName)), x)).ToArray();
        await store.UploadAndDispose(pairs);
        
        var assets = new List<Asset>();
        foreach (var (fileName, file)  in pairs)
        {
            var asset = new Asset(
                CreatedBy: userInfo.Name,
                Path: "/"+ fileName,
                Url: store.GetUrl(fileName),
                Name: file.FileName,
                Title: file.FileName,
                Size: file.Length,
                Type: file.ContentType,
                Metadata: new Dictionary<string, object>());
            assets.Add(asset);
        }
        
        //track those assets to reuse later
        await executor.BatchInsert(Assets.TableName, assets.ToInsertRecords());
        return assets.Select(x => x.Path).ToArray();
    }
    public XEntity GetEntity(bool countLink) =>countLink ? Assets.EntityWithLinkCount: Assets.Entity;

    public async Task UpdateAssetsLinks(string[] newAssetPaths, string entityName, long id, bool checkExisting,
        CancellationToken ct)
    {
        Record[] newLinks = [];
        Record[] existingLinks = [];
        if (newAssetPaths.Length == 0)
        {
            newLinks = await executor.Many(Assets.GetAssetIDsByPaths(newAssetPaths), ct);
            newLinks = await EnsurePathTracked(newAssetPaths, newLinks, ct);
        }

        if (checkExisting)
        {
            existingLinks = await executor.Many(AssetLinks.GetAssetIdsByEntityAndRecordId(entityName, id), ct);
        }

        var (toAdd, toDel) = AssetLinks.Diff(
            newLinks.Select(x => (long)x[nameof(Asset.Id).Camelize()]),
            existingLinks.Select(x => (long)x[nameof(AssetLink.AssetId).Camelize()])
        );

        await executor.BatchInsert(AssetLinks.TableName, AssetLinks.ToInsertRecords(entityName, id, toAdd));

        if (toDel.Length > 0)
        {
            await executor.Exec(AssetLinks.DeleteByEntityAndRecordId(entityName, id, toDel), false, ct);
        }
    }

    private async Task<Record[]> EnsurePathTracked(string[] assetPaths, Record[] assetRecords, CancellationToken ct)
    {
        var set = assetRecords
            .Select(x => x.StrOrEmpty(nameof(Asset.Path).Camelize()))
            .ToHashSet();
        var list = new List<Asset>();
        foreach (var s in assetPaths)
        {
            if (set.Contains(s)) continue;
            var size = await store.GetFileSize(s);
            if (size == 0) continue;

            var asset = new Asset(
                CreatedBy: profileService.GetInfo()?.Name ?? "",
                Path: s,
                Url: store.GetUrl(s),
                Name: s,
                Title: s,
                Size: size,
                Type: await store.GetContentType(s),
                Metadata: new Dictionary<string, object>()
            );
            list.Add(asset);
        }

        if (list.Count <= 0) return assetRecords;
        await executor.BatchInsert(Assets.TableName, list.ToInsertRecords());
        assetRecords = await executor.Many(Assets.GetAssetIDsByPaths(assetPaths), ct);

        return assetRecords;
    }

    private static string GetUniqName(string fileName)
        => string.Concat(Ulid.NewUlid().ToString().AsSpan(0, 12), Path.GetExtension(fileName));
}