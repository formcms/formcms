using FormCMS.Core.Assets;
using FormCMS.Core.HookFactory;
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
    SystemSettings systemSettings,
    IRelationDbDao dao,
    IResizer resizer,
    IServiceProvider provider,
    HookRegistry hookRegistry
) : IAssetService
{
    public async Task EnsureTable()
    {
        await migrator.MigrateTable(Assets.TableName, Assets.Columns);
        await migrator.MigrateTable(AssetLinks.TableName, AssetLinks.Columns);
        await dao.CreateForeignKey(
            AssetLinks.TableName, nameof(AssetLink.AssetId).Camelize(),
            Assets.TableName, nameof(Asset.Id).Camelize(),
            CancellationToken.None);

    }

    public XEntity GetEntity(bool withLinkCount) => withLinkCount ? Assets.EntityWithLinkCount : Assets.Entity;

    public string GetBaseUrl() => systemSettings.AssetUrlPrefix;

    public async Task<Asset> Single(long id, CancellationToken ct = default)
    {
        await hookRegistry.AssetPreSingle.Trigger(provider,new AssetPreSingleArgs(id));
        
        var record = await executor.Single(Assets.Single(id), ct);
        var asset = record?.ToObject<Asset>().Ok() ?? throw new ResultException("Asset not found");
        var links = await executor.Many(AssetLinks.LinksByAssetId([id]), ct);
        var assetLinks = links.Select(x => x.ToObject<AssetLink>().Ok()).ToArray();
        return asset with { Links = assetLinks, LinkCount = links.Length };
    }

    public async Task<ListResponse> List(StrArgs args, int? offset, int? limit, bool withLinkCount, CancellationToken ct)
    {

        var (filters, sorts) = QueryStringParser.Parse(args);
        var res =await hookRegistry.AssetPreList.Trigger(provider,new AssetPreListArgs([..filters]));
        filters = [..res.RefFilters];
        
        var query = Assets.List(offset, limit);
        var items = await executor.Many(query, Assets.Columns, filters, sorts, ct);
        if (withLinkCount) await LoadLinkCount(items, ct);
        var count = await executor.Count(Assets.Count(), Assets.Columns, filters, ct);
        return new ListResponse(items, count);
    }

    public async Task<string[]> Add(IFormFile[] files)
    {
        foreach (var formFile in files)
        {
            if (formFile.Length == 0) throw new ResultException($"File [{formFile.FileName}] is empty");
        }

        files = files.Select(resizer.CompressImage).ToArray();
        var dir = DateTime.Now.ToString("yyyy-MM");
        var pairs = files.Select(x => (Path.Join(dir, GetUniqName(x.FileName)), x)).ToArray();
        await store.Upload(pairs);

        var assets = new List<Asset>();
        foreach (var (fileName, file) in pairs)
        {
            var asset = new Asset(
                CreatedBy: "",
                Path: "/" + fileName,
                Url: store.GetUrl(fileName),
                Name: file.FileName,
                Title: file.FileName,
                Size: file.Length,
                Type: file.ContentType,
                Metadata: new Dictionary<string, object>());
            var res =await hookRegistry.AssetPreAdd.Trigger(provider, new AssetPreAddArgs(asset));
            asset = res.RefAsset;
            assets.Add(asset);
        }

        //track those assets to reuse later
        await executor.BatchInsert(Assets.TableName, assets.ToInsertRecords());
        return assets.Select(x => x.Path).ToArray();
    }

    public async Task Replace(long id, IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0) throw new ResultException($"File [{file.FileName}] is empty");
        await hookRegistry.AssetPreUpdate.Trigger(provider,new AssetPreUpdateArgs(id));

        //make sure the asset to replace existing
        var asset = await Single(id, ct);
        file = resizer.CompressImage(file);
        using var trans = await dao.BeginTransaction();
        try
        {
            var updateQuery = Assets.UpdateFile(asset.Id, file.FileName, file.Length, file.ContentType);
            await executor.Exec(updateQuery, false, ct);
            await store.Upload([(asset.Path, file)]);
            trans.Commit();
        }
        catch (Exception e)
        {
            trans.Rollback();
            throw e is ResultException ? e : new ResultException(e.Message);
        }
    }

    public Task UpdateMetadata(Asset asset, CancellationToken ct)
        => executor.Exec(asset.UpdateMetaData(), false, ct);

    public async Task Delete(long id, CancellationToken ct)
    {
        await hookRegistry.AssetPreUpdate.Trigger(provider,new AssetPreUpdateArgs(id));
        var asset = await Single(id, ct);
        using var trans = await dao.BeginTransaction();
        try
        {
            await executor.Exec(Assets.Deleted(id), false, ct);
            await store.Del(asset.Path);
            trans.Commit();
        }
        catch (Exception e)
        {
            trans.Rollback();
            throw e is ResultException ? e : new ResultException(e.Message);
        }
    }


    public async Task UpdateAssetsLinks(Record[]oldLinks, string[] newAssets, string entityName, long id, CancellationToken ct)
    {
        Record[] newLinks = [];
        if (newAssets.Length > 0)
        {
            newLinks = await executor.Many(Assets.GetAssetIDsByPaths(newAssets), ct);
            newLinks = await EnsurePathTracked(newAssets, newLinks, ct);
        }

        var (toAdd, toDel) = AssetLinks.Diff(
            newLinks.Select(x => (long)x[nameof(Asset.Id).Camelize()]),
            oldLinks.Select(x => (long)x[nameof(AssetLink.AssetId).Camelize()])
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
            if (set.Contains(s) || !Assets.IsValidPath(s)) continue;

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
            item[nameof(Asset.LinkCount).Camelize()] = dict.TryGetValue(id, out var val) ? val : 0;
        }
    }

}