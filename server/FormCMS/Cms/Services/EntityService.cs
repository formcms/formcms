using System.Text.Json;
using FluentResults;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Assets;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.Cache;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using DataType = FormCMS.Core.Descriptors.DataType;
using Task = System.Threading.Tasks.Task;

namespace FormCMS.Cms.Services;

public sealed class EntityService(
    IRelationDbDao relationDbDao,
    KateQueryExecutor executor,
    IEntitySchemaService entitySchemaSvc,
    IAssetService assetService,
    IServiceProvider provider,
    KeyValueCache<long> maxRecordIdCache,
    HookRegistry hookRegistry,
    PluginRegistry pluginRegistry
) : IEntityService
{
    public async Task<Result<LoadedEntity>> GetEntityAndValidateRecordId(
        string entityName,
        long recordId,
        CancellationToken ct
    )
    {
        if (!pluginRegistry.PluginEntities.TryGetValue(entityName, out var entity))
        {
            var entities = await entitySchemaSvc.AllEntities(ct);
            entity = entities.FirstOrDefault(x => x.Name == entityName);
            if (entity is null) throw new ResultException("Entity not found");
        }

        var maxId = await maxRecordIdCache.GetOrSet(entityName,
            async _ => await relationDbDao.MaxId(entity.TableName, entity.PrimaryKey, ct), ct);

        if (recordId < 1 || recordId > maxId)
        {
            return Result.Fail("Record id is out of range");
        }
        return entity.ToLoadedEntity();
    }

    public async Task<ListResponse?> ListWithAction(
        string name,
        ListResponseMode mode,
        Pagination pagination,
        StrArgs args,
        CancellationToken ct)
    {
        var entity = (await entitySchemaSvc.LoadEntity(name, null, ct)).Ok();
        var (filters, sorts, validPagination) = await GetListArgs(entity, args, pagination);
        return await ListWithAction(entity, mode, filters, sorts, validPagination, ct);
    }

    public async Task<Record[]> ListAsTree(string name, CancellationToken ct)
    {
        var entity = await entitySchemaSvc.LoadEntity(name, null, ct).Ok();
        var parentField = entity.Attributes.FirstOrDefault(x =>
            x.DataType == DataType.Collection && x.GetCollectionTarget(out var entityName, out _) && entityName == name
        ) ?? throw new ResultException(
            "Can not compose list result as tree, not find an collection attribute whose target is the entity.");

        parentField.GetCollectionTarget(out _, out var linkField);
        var attributes = entity.Attributes.Where(
            x => 
                x.Field == entity.PrimaryKey ||
                x.Field == DefaultColumnNames.UpdatedAt.Camelize()
                || x.InList && x.DataType.IsLocal());
        var items = await executor.Many(entity.AllQueryForTree(attributes), ct);
        return items.ToTree(entity.PrimaryKey, linkField);
    }

    public async Task<Record> SingleWithAction(string entityName, string id, CancellationToken ct = default)
    {
        var ctx = await GetIdCtx(entityName, id, ct);
        var res = await hookRegistry.EntityPreGetSingle.Trigger(provider,
            new EntityPreGetSingleArgs(ctx.Entity, ctx.Id, null));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        var attr = ctx.Entity.Attributes
            .Where(x =>
                x.Field == ctx.Entity.PrimaryKey
                || x.Field == DefaultAttributeNames.PublishedAt.Camelize()
                || x.Field == DefaultAttributeNames.PublicationStatus.Camelize()
                || x.Field == DefaultColumnNames.UpdatedAt.Camelize()
                || x.InDetail && x.DataType.IsLocal())
            .ToArray();

        var query = ctx.Entity.ByIdsQuery(
            attr.Select(x => x.AddTableModifier()), [ctx.Id], null);
        var record = await executor.Single(query, ct) ??
                     throw new ResultException($"not find record by [{id}]");

        await LoadItems(attr, [record], ct);

        return record;
    }

    public async Task<Record> InsertWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await InsertWithAction(await GetRecordCtx(name, ele, ct), ct);
    }

    public async Task<Record> UpdateWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await UpdateWithAction(await GetRecordCtx(name, ele, ct), ct);
    }

    public async Task<Record> DeleteWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await Delete(await GetRecordCtx(name, ele, ct), ct);
    }

    public async Task SavePublicationSettings(string name, JsonElement ele, CancellationToken ct)
    {
        var (entity, record) = await GetRecordCtx(name, ele, ct);
        if (!record.TryGetValue(entity.PrimaryKey, out var id) && id is null)
        {
            throw new ResultException($"Failed to get Record Id, cannot find [{name}]");
        }

        var query = entity.SavePublicationStatus(id, record).Ok();
        await executor.Exec(query, false, ct);
    }

    public async Task<LookupListResponse> LookupList(string name, string startsVal, CancellationToken ct = default)
    {
        var (entity, sorts, pagination, attributes) = await GetLookupContext(name, ct);
        var count = await executor.Count(entity.Basic(), ct);
        if (count < entity.DefaultPageSize)
        {
            //not enough for one page, search in a client
            var query = entity.ListQuery([], sorts, pagination, null, attributes, null);
            var items = await executor.Many(query, ct);
            return new LookupListResponse(false, items);
        }

        ValidFilter[] filters = [];
        if (!string.IsNullOrEmpty(startsVal))
        {
            var constraint = new Constraint(Matches.StartsWith, [startsVal]);
            var filter = new Filter(entity.LabelAttributeName, MatchTypes.MatchAll, [constraint]);
            filters = (await FilterHelper.ToValidFilters([filter], entity, null, entitySchemaSvc))
                .Ok();
        }

        var queryWithFilters = entity.ListQuery(filters, sorts, pagination, null, attributes, null);
        var filteredItems = await executor.Many(queryWithFilters, ct);
        return new LookupListResponse(true, filteredItems);
    }

    public async Task<long> JunctionDelete(string name, string id, string attr, JsonElement[] elements,
        CancellationToken ct)
    {
        var ctx = await GetJunctionCtx(name, id, attr, ct);
        var items = elements.Select(ele =>
            ctx.Junction.TargetEntity.Parse(ele).Ok()).ToArray();

        var res = await hookRegistry.JunctionPreDel.Trigger(provider,
            new JunctionPreDelArgs(ctx.Entity, ctx.Id, ctx.Attribute, items));

        var query = ctx.Junction.Delete(ctx.Id, res.RefItems);
        var ret = await executor.Exec(query, false, ct);
        return ret;
    }

    public async Task<long> JunctionSave(string name, string id, string attr, JsonElement[] elements,
        CancellationToken ct)
    {
        var ctx = await GetJunctionCtx(name, id, attr, ct);

        var items = elements
            .Select(ele => ctx.Junction.TargetEntity.Parse(ele).Ok()).ToArray();
        var res = await hookRegistry.JunctionPreAdd.Trigger(provider,
            new JunctionPreAddArgs(ctx.Entity, ctx.Id, ctx.Attribute, items));
        var query = ctx.Junction.Insert(ctx.Id, res.RefItems);

        var ret = await executor.Exec(query, true, ct);
        return ret;
    }

    public async Task<object[]> JunctionTargetIds(string name, string sid, string attr, CancellationToken ct)
    {
        var (_, _, junction, id) = await GetJunctionCtx(name, sid, attr, ct);
        var query = junction.GetTargetIds([id]);
        var records = await executor.Many(query, ct);
        return records.Select(x => x[junction.TargetAttribute.Field]).ToArray();
    }

    public async Task<ListResponse> JunctionList(string name, string sid, string attr, bool exclude,
        Pagination pagination,
        StrArgs args, CancellationToken ct)
    {
        var (_, _, junction, id) = await GetJunctionCtx(name, sid, attr, ct);
        var target = junction.TargetEntity;

        var attrs = target.Attributes
            .Where(x => x.Field == target.PrimaryKey || x.DataType.IsLocal() && x.InList)
            .ToArray();

        var (filters, sorts, validPagination) = await GetListArgs(target, args, pagination);

        var listQuery = exclude
            ? junction.GetNotRelatedItems(attrs, filters, sorts, validPagination, [id])
            : junction.GetRelatedItems(filters, [..sorts], validPagination, null, attrs, [id], null);

        var countQuery = exclude
            ? junction.GetNotRelatedItemsCount(filters, [id])
            : junction.GetRelatedItemsCount(filters, [id]);

        var items = await executor.Many(listQuery, ct);
        await LoadItems(attrs, items, ct);
        return new ListResponse(items, await executor.Count(countQuery, ct));
    }

    public async Task<Record> CollectionInsert(string name, string sid, string attr, JsonElement element,
        CancellationToken ct = default)
    {
        var (collection, id) = await GetCollectionCtx(name, sid, attr, ct);
        var item = collection.TargetEntity.Parse(element).Ok();
        item[collection.LinkAttribute.Field] = id.ObjectValue!;
        return await InsertWithAction(new RecordContext(collection.TargetEntity, item), ct);
    }

    public async Task<ListResponse> CollectionList(string name, string sid, string attr, Pagination pagination,
        StrArgs args, CancellationToken ct = default)
    {
        var (collection, id) = await GetCollectionCtx(name, sid, attr, ct);
        var (filters, sorts, validPagination) = await GetListArgs(collection.TargetEntity, args, pagination);

        var attributes = collection.TargetEntity.Attributes
            .Where(x => x.Field == collection.TargetEntity.PrimaryKey || x.DataType.IsLocal() && x.InList)
            .ToArray();

        var listQuery = collection.List(filters, sorts, validPagination, null, attributes, [id], null);
        var items = await executor.Many(listQuery, ct);
        await LoadItems(attributes, items, ct);

        var countQuery = collection.Count(filters, [id]);
        return new ListResponse(items, await executor.Count(countQuery, ct));
    }


    private async Task<ListResponse?> ListWithAction(

        LoadedEntity entity,
        ListResponseMode mode,
        ValidFilter[] filters,
        ValidSort[] sorts,
        ValidPagination pagination,
        CancellationToken ct)
    {
        var args = new EntityPreGetListArgs(
            Entity: entity,
            RefFilters: [..filters],
            RefSorts: [..sorts],
            RefPagination: pagination
        );

        var res = await hookRegistry.EntityPreGetList.Trigger(provider, args);
        var attributes = entity.Attributes
            .Where(x =>
                x.Field == entity.PrimaryKey
                || x.Field == DefaultColumnNames.UpdatedAt.Camelize()
                || x.InList && x.DataType.IsLocal()
            ).ToArray();

        var countQuery = entity.CountQuery([..res.RefFilters], null);
        return mode switch
        {
            ListResponseMode.Count => new ListResponse([], await executor.Count(countQuery, ct)),
            ListResponseMode.Items => new ListResponse(await RetrieveItems(), 0),
            _ => new ListResponse(await RetrieveItems(), await executor.Count(countQuery, ct))
        };


        async Task<Record[]> RetrieveItems()
        {
            var listQuery = entity.ListQuery([..res.RefFilters], [..res.RefSorts], res.RefPagination, null, attributes, null);
            var items = await executor.Many(listQuery, ct);
            await LoadItems(attributes, items, ct);
            return items;
        }
    }

    private async Task LoadItems(IEnumerable<LoadedAttribute> attr, Record[] items, CancellationToken ct)
    {
        if (items.Length == 0) return;
        foreach (var attribute in attr)
        {
            if (attribute.DataType == DataType.Lookup)
            {
                await LoadLookupData(attribute, items, ct);
            }
            else 
            {
                attribute.FormatForDisplay(items);
            }
        }
    }

    private async Task LoadLookupData(LoadedAttribute attr, Record[] items, CancellationToken token)
    {
        var ids = attr.GetUniq(items);
        if (ids.Length == 0) return;

        var lookup = attr.Lookup ??
                     throw new ResultException($"not find lookup entity from {attr.AddTableModifier()}");

        var query = lookup.LookupTitleQuery(ids);

        var targetRecords = await executor.Many(query, token);
        foreach (var lookupRecord in targetRecords)
        {
            var lookupId = lookupRecord[lookup.TargetEntity.PrimaryKey];
            foreach (var item in items.Where(local =>
                         local[attr.Field] is not null && local[attr.Field].Equals(lookupId)))
            {
                item[attr.Field] = lookupRecord;
            }
        }
    }

    private async Task<Record> UpdateWithAction(RecordContext ctx, CancellationToken ct)
    {
        var (entity, record) = ctx;

        ResultExt.Ensure(entity.ValidateLocalAttributes(record));
        ResultExt.Ensure(entity.ValidateTitleAttributes(record));

        var res = await hookRegistry.EntityPreUpdate.Trigger(provider,
            new EntityPreUpdateArgs(entity, record));

        record = res.RefRecord;

        //to prevent SqlServer 'Cannot update identity column' error 
        if (!record.Remove(entity.PrimaryKey, out var value) || value is not long id)
        {
            throw new ResultException($"Failed to get id value with primary key [${entity.PrimaryKey}]");
        }

        using var trans = await relationDbDao.BeginTransaction();
        try
        {
            var query = entity.UpdateQuery(id, record).Ok();

            var affected = await executor.Exec(query, false, ct);
            if (affected == 0)
            {
                throw new ResultException(
                    "Error: Concurrent Update Detected. Someone else has modified this item since you last accessed it. Please refresh the data and try again.");
            }

            var oldLinks = await executor.Many(AssetLinks.GetAssetIdsByEntityAndRecordId(entity.Name, id), ct);
            await assetService.UpdateAssetsLinks(oldLinks,entity.GetAssets(record), entity.Name, id, ct);
            trans.Commit();

            await hookRegistry.EntityPostUpdate.Trigger(provider, new EntityPostUpdateArgs(entity, record));
            return record;
        }
        catch (Exception e)
        {
            trans.Rollback();
            throw e is ResultException ? e : new ResultException(e.Message);
        }
    }

    private async Task<Record> InsertWithAction(RecordContext ctx, CancellationToken ct)
    {
        var (entity, record) = ctx;
        ResultExt.Ensure(entity.ValidateLocalAttributes(record));
        ResultExt.Ensure(entity.ValidateTitleAttributes(record));

        var res = await hookRegistry.EntityPreAdd.Trigger(provider,
            new EntityPreAddArgs(entity, record));
        record = res.RefRecord;

        var trans = await relationDbDao.BeginTransaction();
        try
        {

            var id = await executor.Exec(entity.Insert(record), true, ct);

            await assetService.UpdateAssetsLinks([],entity.GetAssets(record), entity.Name, id, ct);
            record[entity.PrimaryKey] = id;
            trans.Commit();

            await hookRegistry.EntityPostAdd.Trigger(provider, new EntityPostAddArgs(entity, record));
            return record;
        }
        catch (Exception ex)
        {
            trans.Rollback();
            throw new ResultException(ex.Message);
        }
    }

    private async Task<Record> Delete(RecordContext ctx, CancellationToken ct)
    {
        var (entity, record) = ctx;

        var res = await hookRegistry.EntityPreDel.Trigger(provider,
            new EntityPreDelArgs(entity, record));
        record = res.RefRecord;


        if (!record.TryGetValue(entity.PrimaryKey, out var val) || val is not long id)
        {
            throw new ResultException($"Failed to get id value with primary key [${entity.PrimaryKey}]");
        }


        var transaction = await relationDbDao.BeginTransaction();
        try
        {
            var affected = await executor.Exec(entity.DeleteQuery(id, record).Ok(), false, ct);
            if (affected == 0)
            {
                throw new ResultException(
                    "Error: Concurrent Write Detected. Someone else has modified this item since you last accessed it. Please refresh the data and try again.");
            }

            var oldLinks = await executor.Many(AssetLinks.GetAssetIdsByEntityAndRecordId(entity.Name, id), ct);
            await assetService.UpdateAssetsLinks(oldLinks,[], entity.Name, id, ct);

            transaction.Commit();

            await hookRegistry.EntityPostDel.Trigger(provider, new EntityPostDelArgs(entity, record));
            return record;

        }
        catch (Exception e)
        {
            transaction.Rollback();
            throw e is ResultException ? e : new ResultException(e.Message);
        }
    }

    record IdContext(LoadedEntity Entity, ValidValue Id);

    private async Task<IdContext> GetIdCtx(string entityName, string id, CancellationToken token)
    {
        var entity = (await entitySchemaSvc.LoadEntity(entityName, null, token)).Ok();
        if (!entity.PrimaryKeyAttribute.ResolveVal(id, out var idValue))
        {
            throw new ResultException($"Failed to cast {id} to {entity.PrimaryKeyAttribute.DataType}");
        }

        return new IdContext(entity, idValue!.Value);
    }

    private record CollectionContext(Collection Collection, ValidValue Id);

    private async Task<CollectionContext> GetCollectionCtx(string entity, string sid, string attr, CancellationToken ct)
    {
        var loadedEntity = (await entitySchemaSvc.LoadEntity(entity, null, ct)).Ok();
        var collection = loadedEntity.Attributes.FirstOrDefault(x => x.Field == attr)?.Collection ??
                         throw new ResultException(
                             $"Failed to get Collection Context, cannot find [{attr}] in [{entity}]");

        if (!loadedEntity.PrimaryKeyAttribute.ResolveVal( sid, out var id))
        {
            throw new ResultException($"Failed to cast {sid} to {loadedEntity.PrimaryKeyAttribute.DataType}");
        }

        return new CollectionContext(collection, id!.Value);
    }


    private record JunctionContext(LoadedEntity Entity, LoadedAttribute Attribute, Junction Junction, ValidValue Id);


    private async Task<JunctionContext> GetJunctionCtx(string entity, string sid, string attr, CancellationToken ct)
    {
        var loadedEntity = (await entitySchemaSvc.LoadEntity(entity, null, ct)).Ok();
        var errMessage = $"Failed to Get Junction Context, cannot find [{attr}] in [{entity}]";
        var attribute = loadedEntity.Attributes.FirstOrDefault(x => x.Field == attr) ??
                        throw new ResultException(errMessage);

        var junction = attribute.Junction ?? throw new ResultException(errMessage);
        if (!junction.SourceAttribute.ResolveVal( sid, out var id))
        {
            throw new ResultException($"Failed to cast {sid} to {junction.SourceAttribute.DataType}");
        }

        return new JunctionContext(loadedEntity, attribute, junction, id!.Value);
    }

    private record RecordContext(LoadedEntity Entity, Record Record);

    private async Task<RecordContext> GetRecordCtx(string name, JsonElement ele, CancellationToken token)
    {
        var entity = (await entitySchemaSvc.LoadEntity(name, null, token)).Ok();
        var record = entity.Parse(ele).Ok();
        return new RecordContext(entity, record);
    }

    private record LookupContext(
        LoadedEntity Entity,
        ValidSort[] Sorts,
        ValidPagination Pagination,
        LoadedAttribute[] Attributes);

    private async Task<LookupContext> GetLookupContext(string name, CancellationToken ct = default)
    {
        var entity = (await entitySchemaSvc.LoadEntity(name, null, ct)).Ok();
        var sort = new Sort(entity.LabelAttributeName, SortOrder.Asc);
        var validSort = (await SortHelper.ToValidSorts([sort], entity, entitySchemaSvc, null)).Ok();
        var pagination = PaginationHelper.ToValid(new Pagination(), entity.DefaultPageSize);
        return new LookupContext(entity, validSort, pagination, [entity.PrimaryKeyAttribute, entity.LabelAttribute]);
    }

    private record ListArgs(ValidFilter[] Filters, ValidSort[] Sorts, ValidPagination Pagination);

    private async Task<ListArgs> GetListArgs(LoadedEntity entity, StrArgs args, Pagination pagination)
    {
        var (filters, sorts) = QueryStringParser.Parse(args);
        var validFilters = await filters.ToValidFilters(entity, null, entitySchemaSvc).Ok();
        var validSort = await sorts.ToValidSorts(entity, entitySchemaSvc, null).Ok();

        var validPagination = PaginationHelper.ToValid(pagination, entity.DefaultPageSize);
        return new ListArgs(validFilters, validSort, validPagination);
    }
}