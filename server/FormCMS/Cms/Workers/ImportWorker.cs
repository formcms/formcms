using System.Collections.Immutable;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Assets;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using Query = SqlKata.Query;

namespace FormCMS.Cms.Workers;
public record ImportWorkerOptions(int DelaySeconds);

public class ImportWorker(
    ImportWorkerOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportWorker> logger,
    ILoggerFactory logFactory,
    IFileStore fileStore
) : TaskWorker(serviceScopeFactory: scopeFactory, logger: logger,delaySeconds: options.DelaySeconds)
{
    protected override async Task DoTask(
        IServiceScope serviceScope, KateQueryExecutor destinationExecutor,
        SystemTask task, CancellationToken ct)
    {
        task.GetPaths().ExtractTaskFile();
        
        var sourceConnection = task.GetPaths().CreateConnection();
        var sourceDao = new SqliteDao(sourceConnection, new Logger<SqliteDao>(logFactory));
        var sourceExecutor = new KateQueryExecutor(sourceDao, new KateQueryExecutorOption(300));
        var destMigrator = serviceScope.ServiceProvider.GetRequiredService<DatabaseMigrator>();

        var (allSchemas, allEntities, entityNameToEntity, allJunctions) = await LoadData();
        var attributeToLookupEntity = GetAttributeToLookup();

        //data is too big to put into a transaction 
       
        await ImportSchemas();
        await ImportEntities();
        await ImportJunctions();
        await ImportAssets();
        await ImportAssetLinks();
        task.GetPaths().Clean();

        return;

        async Task ImportAssets()
        {
            await destMigrator.MigrateTable(Assets.TableName, Assets.Columns.EnsureColumn(DefaultColumnNames.ImportKey,ColumnType.String));
            await KateQueryExecutor.GetPageDataAndInsert(
                sourceExecutor,
                destinationExecutor,
                Assets.TableName,
                nameof(Asset.Id).Camelize(),
                Assets.XEntity.Attributes.Select(x => x.Field),
                async records =>
                {
                    RenamePrimaryKeyToImportKey(records, nameof(Asset.Id).Camelize());
                    ConvertDateTime(records, [nameof(Asset.CreatedAt).Camelize(),nameof(Asset.UpdatedAt).Camelize()]);
                    
                    foreach (var rec in records)
                    {
                        var path = rec.StrOrEmpty(nameof(Asset.Path).Camelize());
                        if (string.IsNullOrEmpty(path)) continue;
                        
                        var local = Path.Join(task.GetPaths().Folder, path);
                        await fileStore.Upload(local, path,ct);
                        rec[nameof(Asset.Url).Camelize()] = fileStore.GetUrl(path);
                    } 
                },
                ct
            );
        }

        async Task ImportAssetLinks()
        {
            await KateQueryExecutor.GetPageDataAndInsert(
                sourceExecutor,
                destinationExecutor,
                AssetLinks.TableName,
                nameof(Asset.Id).Camelize(),
                AssetLinks.Entity.Attributes.Select(x => x.Field),
                async records =>
                {
                    ConvertDateTime(records, [nameof(DefaultColumnNames.CreatedAt).Camelize(),nameof(DefaultColumnNames.UpdatedAt).Camelize()]);
                    foreach (var record in records)
                    {
                        record.Remove(nameof(Asset.Id).Camelize());
                    }
                    await MapAssetLinks(records);
                },
                ct
            );
        }

        async Task MapAssetLinks(Record[] records)
        {
            var entityNameToRecordArray = new Dictionary<string, string[]>();
            foreach (var record in records)
            {
                var entityName = (string)record[nameof(AssetLink.EntityName).Camelize()];
                var recordId = record.StrOrEmpty(nameof(AssetLink.RecordId).Camelize());
                if (entityNameToRecordArray.ContainsKey(entityName))
                {
                    entityNameToRecordArray[entityName] = [..entityNameToRecordArray[entityName], recordId];
                }
                else
                {
                    entityNameToRecordArray[entityName] = [recordId];
                }
            }

            var entityNameToLookupDict = new Dictionary<string, Dictionary<string, object>>();
            foreach (var (key, ids) in entityNameToRecordArray)
            {
                var entity = entityNameToEntity[key];
                var dictImportKeyToId = await destinationExecutor.LoadDict(
                    new Query(entity.TableName).WhereIn(DefaultColumnNames.ImportKey.Camelize(),ids),
                    DefaultColumnNames.ImportKey.Camelize(),
                    entity.PrimaryKey,
                     ct
                );
                entityNameToLookupDict[key] = dictImportKeyToId;

            }

            var assetIds = records.Select(x => x.StrOrEmpty(nameof(AssetLink.AssetId).Camelize()));
            var dictAssetImportKeyToId = await destinationExecutor.LoadDict(
                    new Query(Assets.TableName).WhereIn(DefaultColumnNames.ImportKey.Camelize(),assetIds),
                DefaultColumnNames.ImportKey.Camelize(),
                nameof(Asset.Id).Camelize(),
                 ct
            );

            foreach (var record in records)
            {
                var entityName = record.StrOrEmpty(nameof(AssetLink.EntityName).Camelize());
                var recordId = record.StrOrEmpty(nameof(AssetLink.RecordId).Camelize());
                var assetId = record.StrOrEmpty(nameof(AssetLink.AssetId).Camelize());
                record[nameof(AssetLink.RecordId).Camelize()] = entityNameToLookupDict[entityName][recordId];
                record[nameof(AssetLink.AssetId).Camelize()] = dictAssetImportKeyToId[assetId];
            }
        }

        async Task<(ImmutableArray<Schema>, ImmutableArray<LoadedEntity>, ImmutableDictionary<string, LoadedEntity>,ImmutableArray<Junction>)> LoadData()
        {
            var records = await sourceExecutor.Many(SchemaHelper.ByNameAndType(null, null, null), ct);
            var schemas = records.Select(x => SchemaHelper.RecordToSchema(x).Ok()).ToArray();
            var entities = schemas
                .Where(x => x.Type == SchemaType.Entity)
                .Select(x => x.Settings.Entity!.ToLoadedEntity()).ToArray();
            var dict = entities.ToDictionary(x => x.Name);
            var junctionDict = new Dictionary<string, Junction>();
            foreach (var entity in entities)
            {
                foreach (var attr in entity.Attributes)
                {
                    if (attr.DataType == DataType.Junction && attr.GetJunctionTarget(out var junctionTarget))
                    {
                        var junction = JunctionHelper.CreateJunction(entity, dict[junctionTarget], attr);
                        junctionDict[junction.JunctionEntity.TableName] = junction;
                    }
                }
            }

            return ([..schemas], [..entities], dict.ToImmutableDictionary(), [..junctionDict.Values]);
        }

        ImmutableDictionary<(string, string), LoadedEntity> GetAttributeToLookup()
        {
            var toLookupEntity = new Dictionary<(string, string), LoadedEntity>();

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
            
            foreach (var junction in allJunctions)
            {
                toLookupEntity[(junction.JunctionEntity.Name, junction.SourceAttribute.Field)] = junction.SourceEntity;
                toLookupEntity[(junction.JunctionEntity.Name, junction.TargetAttribute.Field)] = junction.TargetEntity;
            }

            return toLookupEntity.ToImmutableDictionary();
        }

        async Task ImportSchemas()
        {
            await destMigrator.MigrateTable(SchemaHelper.TableName,
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
                    schema = schema with
                    {
                        SchemaId = find.SchemaId, PublicationStatus = PublicationStatus.Draft, IsLatest = true
                    };
                }
                else
                {
                    schema = schema with { PublicationStatus = PublicationStatus.Published, IsLatest = true };
                }

                var resetQuery = schema.ResetLatest();
                var save = schema.Save();
                await destinationExecutor.ExecBatch([(resetQuery,false), (save,false)], ct);
            }
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

                    await ImportEntity(entityNameToEntity[key], circleReference);
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

        async Task ImportJunctions()
        {
            foreach (var junction in allJunctions)
            {
                await ImportEntity(junction.JunctionEntity, false);
            }
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
                        attribute.GetCollectionTarget(out var entityName, out _);
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

        async Task ImportEntity(LoadedEntity entity, bool circleReference)
        {
            var attrs = entity.Attributes.Where(x => x.DataType.IsLocal());
            var cols = attrs.ToColumns(entityNameToEntity.ToDictionary());
            var fields = cols.Select(x => x.Name).ToArray();
            await destMigrator.MigrateTable(entity.TableName, cols
                .EnsureColumn(DefaultColumnNames.Deleted, ColumnType.Boolean)
                .EnsureColumn(DefaultColumnNames.ImportKey, ColumnType.String)
            );

            await (circleReference ? LoadByTreeLevelAndInsert() : LoadByPageAndInsert());

            async Task LoadByTreeLevelAndInsert()
            {
                var parentField = entity.Attributes
                    .First(f =>
                        attributeToLookupEntity.TryGetValue((entity.Name, f.Field), out var e) && e.Name == entity.Name
                    ).Field;

                var query = new Query(entity.TableName).Select(fields);

                var records = await sourceExecutor.Many(query.Clone().Where(parentField, null), ct);

                while (records.Length > 0)
                {
                    var ids = records.Select(x => x[entity.PrimaryKey]).ToArray();
                    await PreInsert(entity, records);
                    await destinationExecutor.Upsert(entity.TableName,
                        DefaultColumnNames.ImportKey.Camelize(),
                        records);

                    var levelQuery = query.Clone().WhereIn(parentField, ids);
                    records = await sourceExecutor.Many(levelQuery, ct);
                }
            }

            async Task LoadByPageAndInsert()
            {
                const int limit = 1000;
                var query = new Query(entity.TableName)
                    .OrderBy(entity.PrimaryKey)
                    .Select(fields).Limit(limit);
                var records = await sourceExecutor.Many(query, ct);

                while (true)
                {
                    await PreInsert(entity, records);
                    await destinationExecutor.Upsert(entity.TableName,
                        DefaultColumnNames.ImportKey.Camelize(),
                        records);
                    if (records.Length < limit) break;

                    var lastId = records.Last()[entity.PrimaryKey];
                    records = await sourceExecutor.Many(query.Clone().Where(entity.PrimaryKey, ">", lastId), ct);
                }
            }
        }

        void RenamePrimaryKeyToImportKey(Record[] records, string primaryKey)
        {
            foreach (var record in records)
            {
                record[DefaultColumnNames.ImportKey.Camelize()] = record[primaryKey];
                record.Remove(primaryKey);
            }
        }

        void ConvertDateTime(Record[] records, IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                foreach (var rec in records)
                {
                    if (!rec.TryGetValue(field, out var val) || val is not string s) continue;
                    rec[field] = DateTime.Parse(s);
                }
            }
        }

        async Task PreInsert(LoadedEntity entity, Record[] records)
        {
            ConvertDateTime(records, entity.Attributes
                .Where(x => x.DataType == DataType.Datetime)
                .Select(x=>x.Field));
            RenamePrimaryKeyToImportKey(records, entity.PrimaryKey);
            await ReplaceLookupFields();
            async Task ReplaceLookupFields()
            {
                foreach (var attribute in entity.Attributes)
                {
                    if (!attributeToLookupEntity.TryGetValue((entity.Name, attribute.Field), out var lookupEntity))
                        continue;

                    var vals = records.Select(x => x.StrOrEmpty(attribute.Field));
                    var dictImportKeyToId = await destinationExecutor.LoadDict(
                        new Query(lookupEntity.TableName).WhereIn(DefaultColumnNames.ImportKey.Camelize(), vals),
                        DefaultColumnNames.ImportKey.Camelize(),
                        lookupEntity.PrimaryKey,
                        ct
                    );

                    foreach (var record in records)
                    {
                        var importId = record.StrOrEmpty(attribute.Field);
                        if (dictImportKeyToId.TryGetValue(importId, out var id))
                        {
                            record[attribute.Field] = id;
                        }
                    }
                }
            }
        }
    }
    
    protected override TaskType GetTaskType() => TaskType.Import;
}