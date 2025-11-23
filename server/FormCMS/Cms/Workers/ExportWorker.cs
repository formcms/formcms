using FormCMS.Core.Descriptors;
using FormCMS.Core.Assets;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Cms.Workers;

public record ExportWorkerOptions(int DelaySeconds);
public class ExportWorker(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory logFactory,
    ILogger<ExportWorker> logger,
    IFileStore fileStore,
    ExportWorkerOptions options
) : TaskWorker(serviceScopeFactory:scopeFactory,logger:logger,delaySeconds:options.DelaySeconds)
{
    protected override TaskType GetTaskType()
    {
        return TaskType.Export;
    }

    protected override async Task DoTask(IServiceScope serviceScope,IPrimaryDao sourceDao,SystemTask task, CancellationToken ct)
    {
        var destConnection = task.GetPaths().CreateConnection();
        var destDao = new SqliteDao(destConnection, new Logger<SqliteDao>(logFactory));

        var (schemaRecords, entities,entityDict) = await LoadData();
        await ExportSchema();
        await ExportEntities();
        await ExportJunctions();
        await ExportAssets();
        await ExportAssetLinks();
        
        task.GetPaths().Zip();
        await fileStore.Upload(task.GetPaths().FullZip,task.GetPaths().Zip,ct);
        task.GetPaths().Clean();
        return;

        async Task ExportAssets()
        {
            await destDao.MigrateTable(Assets.TableName, Assets.Columns);
            await sourceDao.GetPageDataAndInsert(
                destDao,
                Assets.TableName,
                nameof(Asset.Id).Camelize(),
                Assets.XEntity.Attributes.Select(x => x.Field),
                async records =>
                {
                    foreach (string path in records.Select(x => x[nameof(Asset.Path).Camelize()]))
                    {
                        await fileStore.DownloadFileWithRelated(path, Path.Join(task.GetPaths().Folder, path),ct);
                    }
                },
                ct
            );
        }

        async Task ExportAssetLinks()
        {
             await destDao.MigrateTable(AssetLinks.TableName, AssetLinks.Columns);
             await sourceDao.GetPageDataAndInsert(
                 destDao,
                 AssetLinks.TableName,
                 nameof(AssetLink.Id).Camelize(),
                 AssetLinks.Columns.Select(x=>x.Name),
                 null,
                 ct
             );
        }
        async Task<(Record[], LoadedEntity[], Dictionary<string, LoadedEntity>)> LoadData()
        {
            //export latest schema
            var records = await sourceDao.Many(SchemaHelper.ByNameAndType(null, null, null),ct);
            var arr = records.Select(x => SchemaHelper.RecordToSchema(x).Ok())
                .Where(x => x.Type == SchemaType.Entity)
                .Select(x => x.Settings.Entity!.ToLoadedEntity()).ToArray();

            var dict = arr.ToDictionary(x => x.Name, x => x);
            return (records, arr, dict);
        }

        async Task ExportEntities()
        {
            foreach (var entity in entities)
            {
                await ExportEntity(entity);
            }
        }

        async Task ExportJunctions()
        {
            var dictJunction = new Dictionary<string, LoadedEntity>();
            foreach (var entity in entities)
            {
                foreach (var attr in entity.Attributes)
                {
                    if (attr.DataType == DataType.Junction && attr.GetJunctionTarget(out var junctionTarget))
                    {
                        var junction = JunctionHelper.CreateJunction(entity, entityDict[junctionTarget], attr);
                        dictJunction[junction.JunctionEntity.TableName] = junction.JunctionEntity;
                    }
                }
            }

            foreach (var (_, value) in dictJunction)
            {
                await ExportEntity(value);
            }
        }

        async Task ExportEntity(LoadedEntity entity)
        {
            var attrs = entity.Attributes.Where(x => x.DataType.IsLocal());
            var cols = attrs.ToColumns(entityDict);
            await destDao.MigrateTable(entity.TableName, cols.EnsureColumn(DefaultColumnNames.Deleted,ColumnType.Boolean));
            await sourceDao.GetPageDataAndInsert(destDao, entity.TableName, entity.PrimaryKey, cols.Select(x=>x.Name),null,ct);
        }

        async Task ExportSchema()
        {
            await destDao.MigrateTable(SchemaHelper.TableName, SchemaHelper.Columns);
            foreach (var schemaRecord in schemaRecords)
            {
                schemaRecord.Remove(nameof(Schema.Id).Camelize());
            }
            await sourceDao.BatchInsert(SchemaHelper.TableName, schemaRecords);
        }
    }
}