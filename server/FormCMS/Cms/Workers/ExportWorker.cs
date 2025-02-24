using FormCMS.Core.Descriptors;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using Query = SqlKata.Query;

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

    protected override async Task DoTask(IServiceScope serviceScope,KateQueryExecutor sourceExecutor,SystemTask task, CancellationToken ct)
    {
        var destConnection = task.GetPaths().CreateConnection();
        var destDao = new SqliteDao(destConnection, new Logger<SqliteDao>(logFactory));

        var (destExecutor, destMigrator) = (new KateQueryExecutor(destDao, new KateQueryExecutorOption(300)), new DatabaseMigrator(destDao));
        var (schemaRecords, entities,entityDict) = await LoadData();
        await ExportSchema();
        await ExportEntities();
        await ExportJunctions();
        
        task.GetPaths().Zip();
        await fileStore.Upload(task.GetPaths().FullZip,task.GetPaths().Zip);
        task.GetPaths().Clean();
        return;
        
        async Task<(Record[], LoadedEntity[], Dictionary<string, LoadedEntity>)> LoadData()
        {
            //export latest schema
            var records = await sourceExecutor.Many(SchemaHelper.ByNameAndType(null, null, null),ct);
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
            var attrs = entity.Attributes.Where(x => x.IsLocal());
            var cols = attrs.ToColumns(entityDict);
            await destMigrator.MigrateTable(entity.TableName, cols.EnsureColumn(DefaultColumnNames.Deleted,ColumnType.Boolean));
            await GetPageDataAndInsert(entity, cols.Select(x=>x.Name));
        }

        async Task CopyFiles(LoadedEntity entity, Record[] records)
        {
            foreach (var attr in entity.Attributes)
            {
                if (attr.DisplayType is not (DisplayType.File or DisplayType.Image or DisplayType.Gallery)) continue;
                foreach (var record in records)
                {
                    if (!record.TryGetValue(attr.Field, out var value) || value is not string s) continue;
                    
                    var paths = s.Split(',');
                    foreach (var path in paths)
                    {
                        await fileStore.Download(path, Path.Join(task.GetPaths().Folder, path));
                    }
                }
            }
        }
        
        async Task GetPageDataAndInsert(LoadedEntity entity,IEnumerable<string> fields)
        {
            const int limit = 1000;
            var query = new Query(entity.TableName)
                .Where(DefaultColumnNames.Deleted.Camelize(), false)
                .OrderBy(entity.PrimaryKey)
                .Select(fields).Limit(limit);
            var records = await sourceExecutor.Many(query, ct);
            while (true)
            {
                await CopyFiles(entity, records);
                
                await destExecutor.BatchInsert(entity.TableName, records);
                if (records.Length < limit) break;
                var lastId = records.Last()[entity.PrimaryKey];
                records = await sourceExecutor.Many(query.Where(entity.PrimaryKey, ">", lastId),ct);
            }
        }

        async Task ExportSchema()
        {
            await destMigrator.MigrateTable(SchemaHelper.TableName, SchemaHelper.Columns);
            await destExecutor.BatchInsert(SchemaHelper.TableName, schemaRecords);
        }
    }
}