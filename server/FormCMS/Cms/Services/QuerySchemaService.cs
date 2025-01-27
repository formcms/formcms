using FormCMS.Infrastructure.Cache;
using FormCMS.Cms.Graph;
using FluentResults;
using FluentResults.Extensions;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.GraphTypeConverter;
using FormCMS.Utils.ResultExt;
using GraphQLParser.AST;
using Query = FormCMS.Core.Descriptors.Query;
using Schema = FormCMS.Core.Descriptors.Schema;

namespace FormCMS.Cms.Services;

public sealed class QuerySchemaService(
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    KeyValueCache<LoadedQuery> queryCache,
    SystemSettings systemSettings
) : IQuerySchemaService
{
    public async Task<LoadedQuery> ByGraphQlRequest(Query query, GraphQLField[] fields)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
        {
            return await ToLoadedQuery(query, fields);
        }

        var loadedQuery = await queryCache.GetOrSet(query.Name, SaveToDbAndCache);
        if (loadedQuery.Source != query.Source)
        {
            await queryCache.Remove(query.Name);
            loadedQuery = await queryCache.GetOrSet(query.Name, SaveToDbAndCache);
        }

        return loadedQuery;

        async ValueTask<LoadedQuery> SaveToDbAndCache(CancellationToken ct)
        {
            await SaveQuery(query, ct);
            return await ToLoadedQuery(query, fields, ct);
        }
    }

    public async Task<LoadedQuery> ByNameAndCache(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ResultException("Query name should not be empty");
        var query = await queryCache.GetOrSet(name, async (token) =>
        {
            var schema = await schemaSvc.GetByNameDefault(name, SchemaType.Query, token) ??
                         throw new ResultException($"Cannot find query by name [{name}]");
            var query = schema.Settings.Query ??
                        throw new ResultException($"Query [{name}] has invalid query format");
            var fields = Converter.GetRootGraphQlFields(query.Source).Ok();
            return await ToLoadedQuery(query, fields, token);
        }, ct);
        return query ?? throw new ResultException($"Cannot find query [{name}]");
    }


    public async Task SaveQuery(Query query, CancellationToken ct = default)
    {
        query = query with
        {
            IdeUrl =
            $"{systemSettings.GraphQlPath}?query={Uri.EscapeDataString(query.Source)}&operationName={query.Name}"
        };
        await VerifyQuery(query, ct);
        var schema = new Schema(query.Name, SchemaType.Query, new Settings(Query: query));
        await schemaSvc.AddOrUpdateByNameWithAction(schema, ct);

    }

    public async Task Delete(Schema schema, CancellationToken ct)
    {
        await schemaSvc.Delete(schema.Id, ct);
        if (schema.Settings.Query is not null)
        {
            await queryCache.Remove(schema.Settings.Query.Name, ct);
        }
    }

    public string GraphQlClientUrl()
    {
        return systemSettings.GraphQlPath;
    }

    private async Task<LoadedQuery> ToLoadedQuery(Query query, IEnumerable<GraphQLField> fields,
        CancellationToken ct = default)
    {
        var entity = (await entitySchemaSvc.LoadEntity(query.EntityName, ct)).Ok();
        var selection = (await ParseGraphFields("", entity, fields, null, ct)).Ok();
        var sorts = (await query.Sorts.ToValidSorts(entity, entitySchemaSvc)).Ok();
        var validFilter = (await query.Filters.ToValidFilters(entity, entitySchemaSvc, entitySchemaSvc)).Ok();
        return query.ToLoadedQuery(entity, selection, sorts, validFilter);
    }

    private async Task VerifyQuery(Query? query, CancellationToken ct = default)
    {
        if (query is null)
        {
            throw new ResultException("query is null");
        }

        var entity = (await entitySchemaSvc.LoadEntity(query.EntityName, ct)).Ok();
        (await query.Filters.ToValidFilters(entity, entitySchemaSvc, entitySchemaSvc)).Ok();

        var fields = Converter.GetRootGraphQlFields(query.Source).Ok();
        (await ParseGraphFields("", entity, fields,null, ct)).Ok();
        (await SortHelper.ToValidSorts(query.Sorts, entity, entitySchemaSvc)).Ok();
    }

    private Task<Result<GraphAttribute[]>> ParseGraphFields(
        string prefix,
        LoadedEntity entity,
        IEnumerable<GraphQLField> fields,
        GraphAttribute? parent,
        CancellationToken ct = default)
    {
        return fields.ShortcutMap(async field => await entitySchemaSvc
                .LoadSingleAttrByName(entity, field.Name.StringValue, ct)
                .Map(attr => attr.ToGraph())
                .Map(attr => attr with { Prefix = prefix })
                .Bind(async attr =>
                    attr.IsCompound()
                        ? await LoadChildren(
                            string.IsNullOrEmpty(prefix) ? attr.Field : $"{prefix}.{attr.Field}", attr, field
                        )
                        : attr)
                .Bind(async attr => attr.DataType is DataType.Junction or DataType.Collection
                    ? await LoadArgs(field, attr)
                    : attr))
            .Bind(x =>
            {
                if (x.FirstOrDefault(f => f.Field == entity.PrimaryKey) is null)
                    return Result.Fail(
                        $"Primary key [{entity.PrimaryKey}] not in selection list for entity [{entity.Name}]");
                if (parent?.DataType == DataType.Collection &&
                    parent.GetEntityLinkDesc().Value.TargetAttribute.Field is { } field &&
                    x.FirstOrDefault(x => x.Field == field) is null)
                    return Result.Fail($"Referencing Field [{field}] not in selection list for entity [{entity.Name}]");
                return Result.Ok(x);
            });

        async Task<Result<GraphAttribute>> LoadArgs(GraphQLField field, GraphAttribute graphAttr)
        {
            if (!graphAttr.GetEntityLinkDesc().Try(out var desc, out var err))
                return Result.Fail(err);
            var inputs = field.Arguments?.Select(x => new GraphArgument(x)) ?? [];
            if (!QueryHelper.ParseSimpleArguments(inputs).Try( out var res, out  err)) 
                return Result.Fail(err);
            if (!(await SortHelper.ToValidSorts(res.sorts, desc.TargetEntity, entitySchemaSvc)).Try(out var sorts, out err))
                return Result.Fail(err);
            if (!(await res.filters.ToValidFilters(desc.TargetEntity!, entitySchemaSvc, entitySchemaSvc)).Try(
                    out var filters, out err)) return Result.Fail(err);
            return graphAttr with { Pagination = res.pagination, Filters = [..filters], Sorts = [..sorts] };
        }
        

        Task<Result<GraphAttribute>> LoadChildren(
            string newPrefix, GraphAttribute attr, GraphQLField field
        ) => attr.GetEntityLinkDesc()
            .Bind(desc => ParseGraphFields(newPrefix, desc.TargetEntity, field.SelectionSet!.Selections.OfType<GraphQLField>(),attr, ct))
            .Map(sub => attr with { Selection = [..sub] });
    }
}