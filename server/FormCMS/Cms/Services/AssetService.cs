using FormCMS.Core.Files;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Services;

public class AssetService(
    IFileStore store,
    DatabaseMigrator migrator,
    KateQueryExecutor executor,
    SystemSettings systemSettings  
    ):IAssetService
{
    public string GetBaseUrl() => systemSettings.AssetUrlPrefix;
    
    public async Task EnsureTable()
    {
        await migrator.MigrateTable(Assets.TableName, Assets.Columns);
        await migrator.MigrateTable(AssetLinks.TableName, AssetLinks.Columns);
    }

    public async Task<ListResponse> List(StrArgs args,int? offset, int? limit, CancellationToken ct)
    {
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = Assets.List(offset, limit);
        var items = await executor.Many(query, Assets.Columns,filters,sorts,ct);
        var count = await executor.Count(Assets.Count(),Assets.Columns,filters,ct);
        return new ListResponse(items,count);
    }
    public async Task<string[]> Add(IEnumerable<IFormFile> files)
    {
        var infos = await store.Upload(files).Ok();
        var assets = infos.Select(x => new Asset(
            Path: x.Path, Url: x.Url, 
            Name: x.Name, Title: x.Name, 
            Size: x.FileSize, Type: x.ContentType,
            Metadata: new Dictionary<string, object>(), 
            Links: [])
        );
        
        //track those assets to reuse later
        await executor.BatchInsert(Assets.TableName, assets.ToInsertRecords());
        return infos.Select(x => x.Path).ToArray();
    }
    public XEntity GetEntity() =>Assets.Entity;

}