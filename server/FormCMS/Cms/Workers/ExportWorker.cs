using FormCMS.Core.Descriptors;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using Microsoft.Data.Sqlite;
using Query = SqlKata.Query;

namespace FormCMS.Cms.Workers;

public class ExportWorker(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory,
    ILogger<ExportWorker> logger,
    IFileStore fileStore
) : TaskWorker(serviceScopeFactory:scopeFactory,logger:logger)
{
    protected override TaskType GetTaskType()
    {
        return TaskType.Export;
    }

    protected override async Task DoTask(IServiceScope serviceScope,KateQueryExecutor destinationExecutor,int taskId, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        var (targetExecutor,migrator) = CreateDbAccessor();
        var (schemaRecords, entities,entityDict) = await LoadData();
        await ExportSchema();
        await ExportEntities();
        await ExportJunctions();
        fileStore.Move(TaskHelper.GetTempExportFileName(taskId),TaskHelper.GetExportFileName(taskId));
        return;

        async Task<(Record[], Entity[], Dictionary<string, Entity>)> LoadData()
        {
            var records =
                await destinationExecutor.Many(SchemaHelper.ByNameAndType(null, null, PublicationStatus.Published),ct);
            var arr = records.Select(x => SchemaHelper.RecordToSchema(x).Ok())
                .Where(x => x.Type == SchemaType.Entity)
                .Select(x => x.Settings.Entity!).ToArray();

            var dict = arr.ToDictionary(x => x.Name, x => x);
            return (records, arr, dict);
        }

        async Task ExportEntities()
        {
            foreach (var entity in entities)
            {
                await ExportEntity(entity.ToLoadedEntity());
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
            await migrator.MigrateTable(entity.TableName, cols.EnsureColumn(DefaultColumnNames.Deleted,ColumnType.Boolean));
            await BatchInsert(entity.TableName, entity.PrimaryKey, cols.Select(x=>x.Name));
        }
        
        async Task BatchInsert(string tableName, string primaryKey,IEnumerable<string> fields)
        {
            const int limit = 1000;
            var query = new Query(tableName)
                .Where(DefaultColumnNames.Deleted.Camelize(), false)
                .OrderBy(primaryKey)
                .Select(fields).Limit(limit);
            var records = await destinationExecutor.Many(query, ct);
            while (true)
            {
                await targetExecutor.BatchInsert(tableName, records);
                if (records.Length < limit) break;
                var lastId = records.Last()[primaryKey];
                records = await destinationExecutor.Many(query.Where(primaryKey, ">", lastId),ct);
            }
        }

        (KateQueryExecutor,DatabaseMigrator) CreateDbAccessor()
        {
            var targetDao = new SqliteDao(conn, new Logger<SqliteDao>(loggerFactory));
            return (new KateQueryExecutor(targetDao, new KateQueryExecutorOption(300)),
                new DatabaseMigrator(targetDao));
        }

        SqliteConnection CreateConnection()
        {
            var connectionString = $"Data Source={TaskHelper.GetTempExportFileName(taskId)}";
            var con= new SqliteConnection(connectionString);
            con.Open();
            return con;
        }
        
        async Task ExportSchema()
        {
            await migrator.MigrateTable(SchemaHelper.TableName, SchemaHelper.Columns);
            await targetExecutor.BatchInsert(SchemaHelper.TableName, schemaRecords);
        }
    }
}