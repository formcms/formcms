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

namespace FormCMS.Cms.Services;

public class AssetService(
    IFileStore store,
    IIdentityService identityService,
    IResizer resizer,
    IServiceProvider provider,
    HookRegistry hookRegistry,
    SystemSettings systemSettings,
    ShardGroup shardGroup
) : IAssetService
{
    public XEntity GetEntity(bool withLinkCount)
    {
        if (identityService.GetUserAccess()?.CanAccessAdmin != true)
        {
            throw new ResultException("Access denied");
        }
        return withLinkCount ? Assets.EntityWithLinkCount : Assets.XEntity;
    }

    public string GetBaseUrl() => store.GetUrl("");

    public  Task<Asset> Single(long id, bool loadLinks, CancellationToken ct = default)
        => Single(Assets.Single(id),loadLinks, ct);

    public Task<Asset> Single(string path, bool loadLinks, CancellationToken ct = default)
        => Single(Assets.Single(path),loadLinks, ct);
    
    private async Task<Asset> Single(SqlKata.Query query, bool loadLink, CancellationToken ct = default)
    {
        var record = await shardGroup.PrimaryDao.Single(query, ct);
        var asset = record?.ToObject<Asset>().Ok() ?? throw new ResultException("Asset not found");
        await hookRegistry.AssetPreSingle.Trigger(provider, new AssetPreSingleArgs(asset.Id));
        if (!loadLink) return asset;
        
        var links = await shardGroup.PrimaryDao.Many(AssetLinks.LinksByAssetId([asset.Id]), ct);
        var assetLinks = links.Select(x => x.ToObject<AssetLink>().Ok()).ToArray();
        return asset with { Links = assetLinks, LinkCount = links.Length };
    }

    public async Task<ListResponse> List(StrArgs args, int? offset, int? limit, bool withLinkCount, CancellationToken ct)
    {
        var (filters, sorts) = QueryStringParser.Parse(args);
        var res =await hookRegistry.AssetPreList.Trigger(provider,new AssetPreListArgs([..filters]));
        filters = [..res.RefFilters];
        
        var query = Assets.List(offset, limit);
        var items = await shardGroup.PrimaryDao.Many(query, Assets.Columns, filters, sorts, ct);
        if (withLinkCount) await LoadLinkCount(items, ct);
        var count = await shardGroup.PrimaryDao.Count(Assets.Count(), Assets.Columns, filters, ct);
        return new ListResponse(items, count);
    }

    public async Task<string> AddWithAction(string path, string fileName, CancellationToken ct)
    {
        var userId= identityService.GetUserAccess()?.Id ?? throw new ResultException("Access denied");
        var info = await store.GetMetadata(path, ct)?? throw new ResultException("Metadata not found");
        
        var record = await shardGroup.PrimaryDao.Single(Assets.Single(path), ct);
        if (record is null)
        {
            var asset = new Asset(
                CreatedBy: userId,
                Path: path,
                Url: store.GetUrl(path),
                Name: fileName,
                Title: fileName,
                Size: info.Size,
                Type: info.ContentType,
                Metadata: new Dictionary<string, object>()
            );
        
            var res =await hookRegistry.AssetPreAdd.Trigger(provider, new AssetPreAddArgs(asset));
            asset = res.RefAsset;
            await shardGroup.PrimaryDao.BatchInsert(Assets.TableName, Assets.ToInsertRecords([asset]));
            await hookRegistry.AssetPostAdd.Trigger(provider, new AssetPostAddArgs(asset));
        }
        else
        {
            var updateQuery = Assets.UpdateFile((long)record[nameof(Asset.Id).Camelize()], fileName, info.Size, info.ContentType);
            await shardGroup.PrimaryDao.Exec(updateQuery, ct);
        }
        return path;
    }
    
    public async Task<string[]> BatchUploadAndAdd(IFormFile[] files, CancellationToken ct)
    {
        var userId= identityService.GetUserAccess()?.Id ?? throw new ResultException("Access denied");
        if (files.Any(formFile => !IsValidSignature(formFile)))
        {
            throw new ResultException("Invalid file signature");
        }

        files = files.Select(x=>x.IsImage()?resizer.CompressImage(x):x).ToArray();
        var pairs = files.Select(x => (FileUtils.GetFilePath(x.FileName), x)).ToArray();

        var assets = new List<Asset>();
        foreach (var (path, file) in pairs)
        {
            var asset = new Asset(
                    CreatedBy: userId,
                    Path: path,
                    Url: store.GetUrl(path),
                    Name: file.FileName,
                    Title: file.FileName,
                    Size: file.Length,
                    Type: file.ContentType,
                    Metadata: new Dictionary<string, object>()
                );
            var res =await hookRegistry.AssetPreAdd.Trigger(provider, new AssetPreAddArgs(asset));
            asset = res.RefAsset;
            assets.Add(asset);
        }

        await store.Upload(pairs,ct);
        //track those assets to reuse later
        await shardGroup.PrimaryDao.BatchInsert(Assets.TableName, assets.ToInsertRecords());
        foreach (var asset in assets)
        {
            await hookRegistry.AssetPostAdd.Trigger(provider, new AssetPostAddArgs(asset));
        }
        return assets.Select(x => x.Path).ToArray();
    }

    public async Task Replace(long id, IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0) throw new ResultException($"File [{file.FileName}] is empty");
        await hookRegistry.AssetPreUpdate.Trigger(provider,new AssetPreUpdateArgs(id));

        //make sure the asset to replace existing
        var asset = await Single(id, false,ct);
        file = file.IsImage() ? resizer.CompressImage(file) : file;
        using var trans = await shardGroup.PrimaryDao.BeginTransaction();
        try
        {
            var updateQuery = Assets.UpdateFile(asset.Id, file.FileName, file.Length, file.ContentType);
            await shardGroup.PrimaryDao.Exec(updateQuery, ct);
            await store.Upload([(asset.Path, file)],ct);
            trans.Commit();
        }
        catch (Exception e)
        {
            trans.Rollback();
            throw e is ResultException ? e : new ResultException(e.Message);
        }
    }

    public async Task UpdateMetadata(Asset asset, CancellationToken ct)
    {
        await hookRegistry.AssetPreUpdate.Trigger(provider,new AssetPreUpdateArgs(asset.Id));
        await shardGroup.PrimaryDao.Exec(asset.UpdateMetaData(), ct);
    }

    //foreign key will ensure only orphan assets can be deleted
    public async Task Delete(long id, CancellationToken ct)
    {
        var asset = await Single(id, false, ct);
        await hookRegistry.AssetPreDelete.Trigger(provider,new AssetPreDeleteArgs(asset));
        using var trans = await shardGroup.PrimaryDao.BeginTransaction();
        try
        {
            await shardGroup.PrimaryDao.Exec(Assets.Deleted(id), ct);
            await store.Del(asset.Path,ct);
            await hookRegistry.AssetPostDelete.Trigger(provider,new AssetPostDeleteArgs(asset));
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
            newLinks = await shardGroup.PrimaryDao.Many(Assets.GetAssetIDsByPaths(newAssets), ct);
            newLinks = await EnsurePathTracked(newAssets, newLinks, ct);
        }

        var (toAdd, toDel) = AssetLinks.Diff(
            newLinks.Select(x => (long)x[nameof(Asset.Id).Camelize()]),
            oldLinks.Select(x => (long)x[nameof(AssetLink.AssetId).Camelize()])
        );

        await shardGroup.PrimaryDao.BatchInsert(AssetLinks.TableName, AssetLinks.ToInsertRecords(entityName, id, toAdd));

        if (toDel.Length > 0)
        {
            await shardGroup.PrimaryDao.Exec(AssetLinks.DeleteByEntityAndRecordId(entityName, id, toDel), ct);
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

            var metadata = await store.GetMetadata(s,ct);
            if (metadata is null) continue;

            var asset = new Asset(
                CreatedBy: identityService.GetUserAccess()?.Name ?? "",
                Path: s,
                Url: store.GetUrl(s),
                Name: s,
                Title: s,
                Size: metadata.Size,
                Type: metadata.ContentType,
                Metadata: new Dictionary<string, object>()
            );
            list.Add(asset);
        }

        if (list.Count <= 0) return assetRecords;
        await shardGroup.PrimaryDao.BatchInsert(Assets.TableName, list.ToInsertRecords());
        assetRecords = await shardGroup.PrimaryDao.Many(Assets.GetAssetIDsByPaths(assetPaths), ct);

        return assetRecords;
    }

    private async Task LoadLinkCount(Record[] items, CancellationToken ct)
    {
        var ids = items.Select(x => (long)x[nameof(Asset.Id).Camelize()]);
        var dict = await shardGroup.PrimaryDao.LoadDict(
            AssetLinks.CountByAssetId(ids),
            nameof(AssetLink.AssetId).Camelize(),
            nameof(Asset.LinkCount).Camelize(), ct);
        foreach (var item in items)
        {
            var id = item.StrOrEmpty(nameof(Asset.Id).Camelize());
            item[nameof(Asset.LinkCount).Camelize()] = dict.TryGetValue(id, out var val) ? val : 0;
        }
    }

    public async  Task UpdateHlsProgress(Asset asset, CancellationToken ct)
    {
        await shardGroup.PrimaryDao.Exec(asset.UpdateHlsProgress(), ct);
    }

    public bool IsValidSignature(IFormFile file)
    {
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    
        if (string.IsNullOrEmpty(extension) || !systemSettings.FileSignature.TryGetValue(extension, out var validSignatures))
        {
            return false;
        }
        if (validSignatures.Length == 0) return true;

        var maxSignatureLength = validSignatures.Max(sig => sig.Length);
        var fileHeader = new byte[maxSignatureLength];

        using var stream = file.OpenReadStream();
        var bytesRead = stream.Read(fileHeader, 0, maxSignatureLength);
        
        if (bytesRead < validSignatures.Min(sig => sig.Length))
        {
            return false;
        }

        return validSignatures.Any(signature =>
            bytesRead >= signature.Length && fileHeader.Take(signature.Length).SequenceEqual(signature));
    }

}