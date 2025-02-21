using System.Collections.Immutable;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Microsoft.Data.Sqlite;

namespace FormCMS.Cms.Workers;

public class ImportWorker(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory,
    ILogger<ImportWorker> logger
) : TaskWorker(serviceScopeFactory: scopeFactory, logger: logger)
{
    protected override TaskType GetTaskType()
    {
        return TaskType.Import;
    }

    protected override async Task DoTask(
        IServiceScope serviceScope, KateQueryExecutor destinationExecutor, 
        int taskId, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        var sourceExecutor = CreateExecutor();
        var (allSchemas, allEntities, entityNameToEntity) = await LoadData();
        var attributeToLookupEntity = GetAttributeToLookup();
        var databaseMigrator = serviceScope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
        
        await ImportSchema();
        await ImportEntities();
        await ImportJunctions();
        return;

        async Task ImportJunctions()
        {
            var dict = new Dictionary<string, LoadedEntity>();
            foreach (var entity in allEntities)
            {
                foreach (var attr in entity.Attributes)
                {
                    if (attr.DataType == DataType.Junction && attr.GetJunctionTarget(out var junctionTarget))
                    {
                        var junction = JunctionHelper.CreateJunction(entity, entityNameToEntity[junctionTarget], attr);
                        dict[junction.JunctionEntity.TableName] = junction.JunctionEntity;
                    }
                }
            }

            foreach (var (_, value) in dict)
            {
                await ImportEntity(value,false);
            }
        }

        async Task<(ImmutableArray<Schema>, ImmutableArray<Entity>, ImmutableDictionary<string, Entity>)> LoadData()
        {
            var records =
                await sourceExecutor.Many(SchemaHelper.ByNameAndType(null, null, PublicationStatus.Published), ct);
            var schemas = records.Select(x => SchemaHelper.RecordToSchema(x).Ok()).ToArray();
            var entities = schemas
                .Where(x => x.Type == SchemaType.Entity)
                .Select(x => x.Settings.Entity!).ToArray();
            var dict = entities.ToDictionary(x => x.Name, x => x);
            return ([..schemas], [..entities], dict.ToImmutableDictionary());
        }

        async Task ImportEntities()
        {
            var dict = GetEntityToParents();
            while (true)
            {
                var keysToRemove = new List<string>();
                foreach (var (key, value) in dict)
                {
                    var circleReference = value.Count == 1 && value.Contains(key);
                    if (value.Count != 0 && !circleReference) continue;
                    
                    await ImportEntity(entityNameToEntity[key].ToLoadedEntity(),circleReference);
                    keysToRemove.Add(key);
                }

                if (keysToRemove.Count == 0) break;

                // Remove processed keys
                foreach (var key in keysToRemove)
                {
                    foreach (var (_, hashSet) in dict)
                    {
                        hashSet.Remove(key);
                    }
                    dict.Remove(key);
                }
            }
        }

        async Task ImportEntity(LoadedEntity entity,  bool circleReference)
        {
            var attrs = entity.Attributes.Where(x => x.IsLocal());
            var cols = attrs.ToColumns(entityNameToEntity.ToDictionary());
            var fields = cols.Select(x => x.Name).ToArray();
            await databaseMigrator.MigrateTable(entity.TableName, cols
                .EnsureColumn(DefaultColumnNames.Deleted, ColumnType.Boolean)
                .EnsureColumn(DefaultColumnNames.ImportKey, ColumnType.String)
            );

            await (circleReference ? LoadByTreeLevelAndInsert(entity, fields) : LoadByPageAndInsert(entity, fields));
        }

        async Task LoadByTreeLevelAndInsert(LoadedEntity entity, string[] fields)
        {
            var parentField = entity.Attributes
                .First(f =>
                    attributeToLookupEntity.TryGetValue((entity.Name, f.Field), out var e) && e.Name == entity.Name
                ).Field;
            
            var query = new SqlKata.Query(entity.TableName).Select(fields);
            
            var records = await sourceExecutor.Many(query.Clone().Where(parentField, null), ct);
            
            while (records.Length > 0)
            {
                var ids = records.Select(x => x[entity.PrimaryKey]).ToArray();
                await HandleImportKey(entity,records);
                await destinationExecutor.RemoveIdAndUpsert(entity.TableName, entity.PrimaryKey, DefaultColumnNames.ImportKey.Camelize(), records);
                
                var levelQuery = query.Clone().WhereIn(parentField, ids);
                records = await sourceExecutor.Many(levelQuery, ct);
            }
        }
        
        async Task LoadByPageAndInsert(LoadedEntity entity, IEnumerable<string> fields)
        {
            const int limit = 1000;
            var query = new SqlKata.Query(entity.TableName)
                .OrderBy(entity.PrimaryKey)
                .Select(fields).Limit(limit);
            var records = await sourceExecutor.Many(query, ct);

            while (true)
            {
                await HandleImportKey(entity, records);
                await destinationExecutor.RemoveIdAndUpsert(entity.TableName, entity.PrimaryKey, DefaultColumnNames.ImportKey.Camelize(), records);
                if (records.Length < limit) break;

                var lastId = records.Last()[entity.PrimaryKey];
                records = await sourceExecutor.Many(query.Clone().Where(entity.PrimaryKey, ">", lastId), ct);
            }
        }

        async Task HandleImportKey(LoadedEntity entity, Record[] records)
        {
            foreach (var record in records)
            {
                record[DefaultColumnNames.ImportKey.Camelize()] = record[entity.PrimaryKey];
            }

            foreach (var attribute in entity.Attributes)
            {
                if (!attributeToLookupEntity.TryGetValue((entity.Name, attribute.Field), out var lookupEntity))
                    continue;

                var dictImportKeyToId = await destinationExecutor.LoadDict(
                    lookupEntity.TableName,
                    DefaultColumnNames.ImportKey.Camelize(),
                    lookupEntity.PrimaryKey,
                    records.Select(x => x[attribute.Field]), ct
                );

                foreach (var record in records)
                {
                    var importId = record.GetStrOrEmpty(attribute.Field);
                    if (dictImportKeyToId.TryGetValue(importId, out var id))
                    {
                        record[attribute.Field] = id;
                    }
                }
            }
        }

        async Task ImportSchema()
        {
            await databaseMigrator.MigrateTable(SchemaHelper.TableName,
                SchemaHelper.Columns.EnsureColumn(DefaultColumnNames.ImportKey, ColumnType.String));

            foreach (var t in allSchemas)
            {
                //insert a new version
                var schema = t;
                var findRecord =
                    await destinationExecutor.Single(SchemaHelper.ByNameAndType(schema.Type, [schema.Name], null), ct);
                if (findRecord is not null)
                {
                    var find = SchemaHelper.RecordToSchema(findRecord).Ok();
                    schema = schema with { SchemaId = find.SchemaId };
                }

                schema = schema.Init();
                var resetQuery = schema.ResetLatest();
                var save = schema.Save();
                await destinationExecutor.ExecBatch([resetQuery, save], true, ct);
            }
        }

        ImmutableDictionary<(string, string), Entity> GetAttributeToLookup()
        {
            var toLookupEntity = new Dictionary<(string, string), Entity>();

            foreach (var entity in allEntities)
            {
                foreach (var attribute in entity.Attributes)
                {
                    if (attribute.DataType == DataType.Collection)
                    {
                        attribute.GetCollectionTarget(out var entityName, out var lookupAttr);
                        toLookupEntity[(entityName, lookupAttr)] = entity;
                    }
                    else if (attribute.DataType == DataType.Lookup)
                    {
                        attribute.GetLookupTarget(out var parent);
                        toLookupEntity[(entity.Name, attribute.Field)] = entityNameToEntity[parent];
                    }
                }
            }

            return toLookupEntity.ToImmutableDictionary();
        }


        Dictionary<string, HashSet<string>> GetEntityToParents()
        {
            var toParents = new Dictionary<string, HashSet<string>>();
            foreach (var entity in allEntities)
            {
                toParents.Add(entity.Name, new HashSet<string>());
            }

            foreach (var entity in allEntities)
            {
                foreach (var attribute in entity.Attributes)
                {
                    if (attribute.DataType == DataType.Collection)
                    {
                        attribute.GetCollectionTarget(out var entityName, out  _);
                        toParents[entityName].Add(entity.Name);
                    }
                    else if (attribute.DataType == DataType.Lookup)
                    {
                        attribute.GetLookupTarget(out var parent);
                        toParents[entity.Name].Add(parent);
                    }
                }
            }

            return toParents;
        }
        
        SqliteConnection CreateConnection()
        {
            var connectionString = $"Data Source={TaskHelper.GetTempImportFileName(taskId)}";
            var con = new SqliteConnection(connectionString);
            con.Open();
            return con;
        }

        KateQueryExecutor CreateExecutor()
        {
            var targetDao = new SqliteDao(connection, new Logger<SqliteDao>(loggerFactory));
            return new KateQueryExecutor(targetDao, new KateQueryExecutorOption(300));
        }
        
    }
}