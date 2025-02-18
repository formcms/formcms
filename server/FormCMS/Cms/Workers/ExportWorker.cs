using FormCMS.Core.Descriptors;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.KateQueryExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Microsoft.Data.Sqlite;
using Query = SqlKata.Query;
using TaskStatus = FormCMS.Core.Tasks.TaskStatus;

namespace FormCMS.Cms.Workers;

public class ExportWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILoggerFactory loggerFactory,
    ILogger<ExportWorker> logger,
    IFileStore fileStore
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            logger.LogInformation("Wakeup Export Worker...");

            using var scope = serviceScopeFactory.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<KateQueryExecutor>();

            var query = TaskHelper.GetNewExportTask();
            var record = await executor.Single(query,ct);
            if (record != null)
            {
                var task = record.ToObject<SystemTask>().Ok();
                try
                {
                    await executor.ExecAndGetAffected( TaskHelper.UpdateTaskStatus(task with {TaskStatus=TaskStatus.InProgress,Progress = 50}),ct);
                    await Export(task.Id, executor);
                    await executor.ExecAndGetAffected( TaskHelper.UpdateTaskStatus(task with{TaskStatus = TaskStatus.Finished,Progress = 100}),ct);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await executor.ExecAndGetAffected( TaskHelper.UpdateTaskStatus(task with{TaskStatus = TaskStatus.Failed,Progress = 0}),ct);
                }
            }
            await Task.Delay(1000 * 30, ct); // ✅ Prevents blocking
        }
    }

    private async Task Export(object taskId, KateQueryExecutor executor)
    {
        await using var conn = CreateConnection();
        var (targetExecutor,migrator) = CreateDbAccessor();
        var schemaRecords = await executor.Many(SchemaHelper.ByNameAndType(null, null, PublicationStatus.Published));
        var entities = schemaRecords.Select(x => SchemaHelper.RecordToSchema(x).Ok())
            .Where(x=>x.Type == SchemaType.Entity)
            .Select(x=>x.Settings.Entity!).ToArray();
        
        var entityDict = entities.ToDictionary(x => x.Name, x => x);
        await ExportSchema();
        foreach (var entity in entities)
        {
            await ExportEntity(entity.ToLoadedEntity());
        }

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
        fileStore.Move(TaskHelper.GetExportFileName(taskId),TaskHelper.GetExportFileName(taskId));
        
        return;

        async Task ExportEntity(LoadedEntity entity)
        {
            var attrs = entity.Attributes.Where(x => x.IsLocal());
            var cols = attrs.ToColumns(entityDict);
            await migrator.MigrateTable(entity.TableName, cols.EnsureColumn(DefaultAttributeNames.Deleted,ColumnType.Boolean));
            await BatchInsert(entity.TableName, entity.PrimaryKey, cols.Select(x=>x.Name));
        }
        
        async Task BatchInsert(string tableName, string primaryKey,IEnumerable<string> fields)
        {
            const int limit = 1000;
            var query = new Query(tableName)
                .Where(DefaultAttributeNames.Deleted, false)
                .OrderBy(primaryKey)
                .Select(fields).Limit(limit);
            var records = await executor.Many(query);
            while (true)
            {
                await targetExecutor.BatchInsert(tableName, records);
                if (records.Length < limit) break;
                var lastId = records.Last()[primaryKey];
                records = await executor.Many(query.Where(primaryKey, ">", lastId));
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
            var connectionString = $"Data Source={TaskHelper.GetExportFileName(taskId)}";
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