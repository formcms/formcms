using System.Collections.Immutable;
using FormCMS.Infrastructure.Cache;
using FluentResults;
using FluentResults.Extensions;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;
using SchemaType = FormCMS.Core.Descriptors.SchemaType;

namespace FormCMS.Cms.Services;

public sealed class EntitySchemaService(
    ISchemaService schemaSvc,
    IRelationDbDao dao,
    KeyValueCache<ImmutableArray<Entity>> entityCache,
    HookRegistry hook,
    IServiceProvider provider
) : IEntitySchemaService
{
    public ValueTask<ImmutableArray<Entity>> AllEntities(CancellationToken ct)
    {
        return entityCache.GetOrSet("", async token =>
        {
            var schemas = await schemaSvc.All(SchemaType.Entity, null,PublicationStatus.Published ,token);
            var entities = schemas
                .Where(x => x.Settings.Entity is not null)
                .Select(x => x.Settings.Entity!);
            return [..entities];
        }, ct);
    }

    public async ValueTask<ImmutableArray<Entity>> ExtendedEntities(CancellationToken ct)
    {
        var entities = await AllEntities(ct);
        var res = await hook.ExtendEntity.Trigger( provider,new ExtendingGraphQlFieldArgs(entities) );
        return res.entities;
    }

    public async Task<Schema> AddOrUpdateByName(Entity entity, CancellationToken ct)
    {
        var find = await schemaSvc.GetByNameDefault(entity.Name, SchemaType.Entity,null, ct);
        var schema = ToSchema(entity,find?.SchemaId??"", find?.Id ?? 0);
        return await SaveTableDefine(schema, ct);
    }


    public async Task<Schema> Save(Schema schema, CancellationToken ct)
    {
        var ret = await schemaSvc.SaveWithAction(schema, ct);
        await entityCache.Remove("", ct);
        return ret;
    }

    public async Task Delete(Schema schema, CancellationToken ct)
    {
        await schemaSvc.Delete(schema.Id, ct);
        if (schema.Settings.Entity is not null) await schemaSvc.RemoveEntityInTopMenuBar(schema.Settings.Entity, ct);
        await entityCache.Remove("", ct);
    }


    public async Task<Result<LoadedEntity>> LoadEntity(string name, PublicationStatus?status, CancellationToken ct = default)
        => await GetEntity(name,status, ct)
            .Bind(async x => await LoadAttributes(x.ToLoadedEntity(),status, ct));

    private async Task<Result<LoadedAttribute>> LoadSingleAttribute(
        LoadedEntity entity,
        LoadedAttribute attr,
        PublicationStatus? status,
        CancellationToken ct = default
    ) => attr.DataType switch
    {
        DataType.Junction => await LoadJunction(entity, attr, status, ct),
        DataType.Lookup => await LoadLookup(attr, status, ct),
        DataType.Collection => await LoadCollection(entity, attr,status,ct),
        _ => attr
    };

   
    private DataType ColumnTypeToDataType(ColumnType columnType)
        => columnType switch
        {
            ColumnType.Int => DataType.Int,
            ColumnType.String => DataType.String,
            ColumnType.Text => DataType.Text,
            ColumnType.Datetime => DataType.Datetime,
            _ => throw new ArgumentOutOfRangeException()
        };

    public async Task<Entity?> GetTableDefine(string table, CancellationToken ct)
    {
        var cols = await dao.GetColumnDefinitions(table, ct);
        return new Entity
        (
            PrimaryKey: "", Name: "", DisplayName: "", TableName: "", LabelAttributeName: "",
            Attributes:
            [
                ..cols.Select(x => ToAttribute(x.Name, ColumnTypeToDataType(x.Type)))
            ]
        );

        Attribute ToAttribute(string name, DataType colType)
            => new(
                Field: name,
                Header: name,
                DataType: colType
            );
    }

    public async Task SaveTableDefine(Entity entity, CancellationToken token = default)
    {
        await SaveTableDefine(ToSchema(entity), token);
    }

    public async Task<Schema> SaveTableDefine(Schema schema, CancellationToken ct = default)
    {
        schema = schema with { Name = schema.Settings.Entity!.Name };
        
        //hook function might change the schema
        schema = (await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema))).RefSchema;
        schema = WithDefaultAttr(schema);
        VerifyEntity(schema.Settings.Entity!);
        
        await schemaSvc.NameNotTakenByOther(schema, ct).Ok();
        var cols = await dao.GetColumnDefinitions(schema.Settings.Entity!.TableName, ct);
        ResultExt.Ensure(EnsureTableNotExist(schema, cols));

        using var tx = await dao.BeginTransaction();
        
        try
        {
            schema = await schemaSvc.Save(schema, ct);
            await CreateMainTable(schema.Settings.Entity!, cols, ct);
            await schemaSvc.EnsureEntityInTopMenuBar(schema.Settings.Entity!, ct);
            
            var loadedEntity = await LoadAttributes(schema.Settings.Entity!.ToLoadedEntity(),null, ct).Ok();
            await CreateLookupForeignKey(loadedEntity, ct);
            await CreateCollectionForeignKey(loadedEntity, ct);
            await CreateJunctions(loadedEntity, ct);
            tx.Commit();
            await entityCache.Remove("", ct);
            await hook.SchemaPostSave.Trigger(provider, new SchemaPostSaveArgs(schema));
            return schema;
        }
        catch(Exception ex )
        {
            tx.Rollback();
            throw ex is ResultException ? ex: new ResultException(ex.Message,ex);
        }

        Schema WithDefaultAttr(Schema s)
        {
            var e = s.Settings.Entity ?? throw new ResultException("invalid entity payload");
            return s with
            {
                Settings = new Settings(
                    Entity: e with{Attributes = [..e.Attributes.ToArray().WithDefaultAttr()]}
                )
            };
        }
    }

    public async Task<Result<AttributeVector>> ResolveVector(LoadedEntity entity, string fieldName, PublicationStatus? status)
    {
        var fields = fieldName.Split(".");
        var prefix = string.Join(AttributeVectorConstants.Separator, fields[..^1]);
        var attributes = new List<LoadedAttribute>();
        LoadedAttribute? attr = null;
        for (var i = 0; i < fields.Length; i++)
        {
            //check if fields[i] exists in entity
            if (!(await LoadSingleAttrByName(entity, fields[i],status)).Try(out attr, out var e))
            {
                return Result.Fail(e);
            }

            //don't put the last attribute to ancestor
            if (i >= fields.Length - 1) continue;
            
            if (!attr.GetEntityLinkDesc().Try(out var link, out  e))
            {
                return Result.Fail(e);
            }

            entity = link.TargetEntity;
            attributes.Add(attr);
        }

        return new AttributeVector(fieldName, prefix, [..attributes], attr!);
    }

    public async Task<Result<LoadedAttribute>> LoadSingleAttrByName(
        LoadedEntity entity, 
        string attrName,
        PublicationStatus? status,
        CancellationToken ct = default)
    {
        var loadedAttr = entity.Attributes.FirstOrDefault(x=>x.Field.Camelize()==attrName);
        if (loadedAttr is null)
            return Result.Fail($"Load single attribute fail, cannot find [{attrName}] in [{entity.Name}]");

        return await LoadSingleAttribute(entity, loadedAttr,status, ct);
    }


    private async Task<Result<Entity>> GetEntity(string name, PublicationStatus?status, CancellationToken token = default)
    {
        var item = await schemaSvc.GetByNameDefault(name, SchemaType.Entity, status, token);
        if (item is null)
        {
            return Result.Fail($"Cannot find entity [{name}]");
        }

        var entity = item.Settings.Entity;
        if (entity is null)
        {
            return Result.Fail($"Entity [{name}] is invalid");
        }

        return entity;
    }

    private async Task CreateMainTable(Entity entity, Column[] columns, CancellationToken ct)
    {
        var dict = await GetLookupEntityMap(entity.Attributes);
 
        if (columns.Length > 0) //if table exists, alter table adds columns
        {
            var set = columns.Select(x => x.Name).ToHashSet();
            var missing = entity.Attributes.Where(c => c.DataType.IsLocal()&& !set.Contains(c.Field)).ToArray();
            if (missing.Length > 0)
            {
                var missingCols = missing.ToColumns(dict);
                await dao.AddColumns(entity.TableName, missingCols, ct);
            }
        }
        else
        {
            var newColumns = entity.Attributes.Where(x=>x.DataType.IsLocal()).ToColumns(dict);
            await dao.CreateTable(entity.TableName, newColumns.EnsureColumn(DefaultColumnNames.Deleted,ColumnType.Boolean), ct);
        }
    }

    private async Task CreateLookupForeignKey(LoadedEntity entity,CancellationToken ct)
    {
        foreach (var attr in entity.Attributes.Where(attr=>attr.DataType == DataType.Lookup))
        {
            var targetEntity = attr.Lookup!.TargetEntity;
            await dao.CreateForeignKey(entity.TableName, attr.Field, targetEntity.TableName, targetEntity.PrimaryKey, ct);
        }
    }
    private async Task CreateCollectionForeignKey(LoadedEntity entity,CancellationToken ct)
    {
        foreach (var attr in entity.Attributes.Where(attr=>attr.DataType == DataType.Collection))
        {
            var collection = attr.Collection!;
            await dao.CreateForeignKey(collection.TargetEntity.TableName, collection.LinkAttribute.Field, entity.TableName, entity.PrimaryKey, ct);
        }
    }

    private async Task CreateJunctions(LoadedEntity entity, CancellationToken ct)
    {
        var dict = await GetLookupEntityMap(entity.Attributes);
        foreach (var attribute in entity.Attributes.Where(x => x.DataType == DataType.Junction))
        {
            var junction = attribute.Junction!;
            var columns = await dao.GetColumnDefinitions(junction.JunctionEntity.TableName, ct);
            if (columns.Length == 0)
            {
                var cols =  junction.JunctionEntity.Attributes.ToColumns(dict);
                await dao.CreateTable(junction.JunctionEntity.TableName, cols.EnsureColumn(DefaultColumnNames.Deleted,ColumnType.Boolean), ct);
                await dao.CreateForeignKey(
                    table: junction.JunctionEntity.TableName,
                    col: junction.SourceAttribute.Field,
                    refTable: junction.SourceEntity.TableName,
                    refCol: junction.SourceEntity.PrimaryKey,
                    ct);

                await dao.CreateForeignKey(
                    table: junction.JunctionEntity.TableName,
                    col: junction.TargetAttribute.Field,
                    refTable: junction.TargetEntity.TableName,
                    refCol: junction.TargetEntity.PrimaryKey,
                    ct);
            }
        }
    }

    private static Result EnsureTableNotExist(Schema schema, Column[] columns)
    {
        var creatingNewEntity = schema.Id == 0;
        var tableExists = columns.Length > 0;

        return creatingNewEntity && tableExists
            ? Result.Fail($"Fail to add new entity, the table {schema.Settings.Entity!.TableName} already exists")
            : Result.Ok();
    }

    private async Task<Result<Entity>> GetLookupEntity(Attribute attribute, PublicationStatus?status, CancellationToken ct = default)
        => attribute.GetLookupTarget(out var entity)
            ? await GetEntity(entity, status, ct)
            : Result.Fail($"Lookup target was not set to Attribute [{attribute.Field}]");

    private async Task<Result<LoadedAttribute>> LoadLookup(
        LoadedAttribute attr, PublicationStatus?status, CancellationToken ct
    ) => attr.Lookup switch
    {
        not null => attr,
        _ => await GetLookupEntity(attr, status, ct).Map(x => attr with { Lookup = new Lookup(x.ToLoadedEntity())})
    };

    private async Task<Result<LoadedAttribute>> LoadCollection( 
        LoadedEntity sourceEntity, LoadedAttribute attr, PublicationStatus? status, CancellationToken ct
    )
    {
        if (attr.Collection is not null) return attr;
        
        if (!attr.GetCollectionTarget(out var entityName, out var linkAttrName))
            return Result.Fail( $"Target of Collection attribute [{attr.Field}] not set.");

        return await GetEntity(entityName,status, ct).Bind(async entity =>
        {

            var loadedEntity = entity.ToLoadedEntity();
            if (!(await LoadLookups(loadedEntity,status, ct)).Try(out loadedEntity, out var err))
            {
                return Result.Fail(err);
            }

            var loadAttribute = loadedEntity.Attributes.FirstOrDefault(x=>x.Field ==linkAttrName);
            if (loadAttribute is null) return Result.Fail($"Not found [{linkAttrName}] from entity [{entityName}]");
            
            var collection = new Collection(sourceEntity, loadedEntity,loadAttribute );
            return Result.Ok(collection);
        }).Map(c => attr with{Collection = c});
    }

    private async Task<Dictionary<string, LoadedEntity>> GetLookupEntityMap(IEnumerable<Attribute> attributes)
    {
        var ret = new Dictionary<string, LoadedEntity>();
        foreach (var attribute in attributes.Where(x=>x.DataType == DataType.Lookup))
        {
            var lookup = await GetLookupEntity(attribute, null).Ok();
            ret[lookup.Name] = lookup.ToLoadedEntity();
        }
        return ret;
    }
    private Task<Result<LoadedEntity>> LoadLookups(LoadedEntity entity, PublicationStatus? status, CancellationToken ct = default)
        => entity.Attributes
            .ShortcutMap(async attr =>
                attr is { DataType: DataType.Lookup} ? await LoadLookup(attr, status,ct) : attr)
            .Map(x => entity with { Attributes = [..x] });

    private async Task<Result<LoadedAttribute>> LoadJunction(
        LoadedEntity entity, LoadedAttribute attr, PublicationStatus?status, CancellationToken ct
    )
    {
        if (attr.Junction is not null) return attr;

        if (!attr.GetJunctionTarget(out var targetName))
            return Result.Fail($"Junction Option was not set for attribute `{entity.Name}.{attr.Field}`");

        return await GetEntity(targetName, status, ct)
            .Map(e => e.ToLoadedEntity())
            .Bind(e => LoadLookups(e,status, ct))
            .Map(x => attr with { Junction = JunctionHelper.CreateJunction(entity, x, attr) })
            .OnFail($"Failed to load Junction for attribute {attr.Field}");
    }

    private Task<Result<LoadedEntity>> LoadAttributes(
        LoadedEntity entity, 
        PublicationStatus? status, 
        CancellationToken ct
    ) => entity.Attributes
        .ShortcutMap(x => LoadSingleAttribute(entity, x,status,ct))
        .Map(x => entity with { Attributes = [..x] });

    private void VerifyEntity(Entity entity)
    {
        var msg = $"Verification of the entity [{entity.Name}] failed,";
        foreach (var attr in entity.Attributes)
        {
            if (!DataTypeHelper.ValidTypeMap.Contains((attr.DataType, attr.DisplayType)))
            {
                throw new ResultException(
                    $"{msg} The data type=[{attr.DataType}] with display type =[{attr.DisplayType}] for [{attr.Field}] is not supported.");
            }

            if (attr.DisplayType is DisplayType.Dropdown or DisplayType.Multiselect && string.IsNullOrWhiteSpace(attr.Options))
            {
                throw new ResultException(
                    $"{msg} Please input options for  [{attr.Field}] because it's display type is [{attr.DisplayType}] ");
            }
        }

        if (string.IsNullOrEmpty(entity.TableName)) throw new ResultException($"{msg} Table name should not be empty");
        if (string.IsNullOrEmpty(entity.LabelAttributeName)) throw new ResultException($"{msg} Title attribute should not be empty");
        if (string.IsNullOrEmpty(entity.PrimaryKey)) throw new ResultException($"{msg} Primary key should not be empty");

        if (entity.DefaultPageSize < 1) throw new ResultException($"{msg}default page size should be greater than 0");

        _ = entity.Attributes.FirstOrDefault(x=>x.Field ==entity.PrimaryKey) ??
            throw new ResultException($"{msg} [{entity.PrimaryKey}] was not in attributes list");

        _ = entity.Attributes.FirstOrDefault(x=>x.Field==entity.LabelAttributeName) ??
            throw new ResultException($"{msg} [{entity.LabelAttributeName}] was not in attributes list");
    }


    private Schema ToSchema(
        Entity entity, string schemaId="", long id = 0
    ) => new(
        Id: id,
        SchemaId:schemaId,
        Name: entity.Name,
        Type: SchemaType.Entity,
        Settings: new Settings
        (
            Entity: entity
        ),
        CreatedBy: ""
    );
}