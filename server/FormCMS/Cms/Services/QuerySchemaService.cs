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
            return await ToLoadedQuery(query, fields,null);
        }

        var schema = await schemaSvc.GetByNameDefault(query.Name, SchemaType.Query, null,CancellationToken.None);
        if (schema == null || schema.Settings.Query != null && schema.Settings.Query.Source != query.Source)
        {
            await SaveQuery(query, null);
        }
        return await ToLoadedQuery(query, fields,null);
    }

    public async Task<LoadedQuery> ByNameAndCache(string name, PublicationStatus? status, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ResultException("Query name should not be empty");
        var query = status == PublicationStatus.Published
            ? await queryCache.GetOrSet(name, GetQuery, ct)
            : await GetQuery(ct);
        
        return query ?? throw new ResultException($"Cannot find query [{name}]");

        async ValueTask<LoadedQuery> GetQuery(CancellationToken token)
        {
            var schema = await schemaSvc.GetByNameDefault(name, SchemaType.Query, status, token) ??
                         throw new ResultException($"Cannot find query by name [{name}]");
            var settingsQuery = schema.Settings.Query ??
                                throw new ResultException($"Query [{name}] has invalid query format");
            var fields = Converter.GetRootGraphQlFields(settingsQuery.Source).Ok();
            return await ToLoadedQuery(settingsQuery, fields, status, token);
        }
    }


    public async Task SaveQuery(Query query, PublicationStatus?status, CancellationToken ct = default)
    {
        query = query with
        {
            IdeUrl =
            $"{systemSettings.GraphQlPath}?query={Uri.EscapeDataString(query.Source)}"
        };
        await VerifyQuery(query, status, ct);
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

    private async Task<LoadedQuery> ToLoadedQuery(
        Query query, 
        IEnumerable<GraphQLField> fields,
        PublicationStatus? status,
        CancellationToken ct = default)
    {
        var entity = await entitySchemaSvc.LoadEntity(query.EntityName,status, ct).Ok();
        var selection = await ParseGraphFields("", entity, fields, null, status, ct).Ok();
        var sorts = await query.Sorts.ToValidSorts(entity, entitySchemaSvc,status).Ok();
        var validFilter = await query.Filters.ToValidFilters(entity,status, entitySchemaSvc, entitySchemaSvc).Ok();
        return query.ToLoadedQuery(entity, selection, sorts, validFilter);
    }

    private async Task VerifyQuery(Query? query, PublicationStatus? status, CancellationToken ct = default)
    {
        if (query is null)
        {
            throw new ResultException("query is null");
        }

        var entity = await entitySchemaSvc.LoadEntity(query.EntityName,status, ct).Ok();
        await query.Filters.ToValidFilters(entity, status, entitySchemaSvc, entitySchemaSvc).Ok();

        var fields = Converter.GetRootGraphQlFields(query.Source).Ok();
        await ParseGraphFields("", entity, fields,null,status, ct).Ok();
        await query.Sorts.ToValidSorts(entity, entitySchemaSvc,status).Ok();
    }

    private Task<Result<GraphAttribute[]>> ParseGraphFields(
        string prefix,
        LoadedEntity entity,
        IEnumerable<GraphQLField> fields,
        GraphAttribute? parent,
        PublicationStatus? status,
        CancellationToken ct = default)
    {
        return fields.ShortcutMap( async field =>
                    await entitySchemaSvc.LoadSingleAttrByName(entity, field.Name.StringValue, status, ct)
                        .Map(attr => attr.ToGraph(attr.IsAsset()?GetAssetFields(field): []))
                        .Map(attr => attr with { Prefix = prefix })
                        .Bind(
                            async attr => attr.IsCompound()
                                ? await LoadChildren(
                                    string.IsNullOrEmpty(prefix) ? attr.Field : $"{prefix}.{attr.Field}", attr, field)
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
                    x.FirstOrDefault(attribute => attribute.Field == field) is null)
                    return Result.Fail($"Referencing Field [{field}] not in selection list for entity [{entity.Name}]");
                return Result.Ok(x);
            });

        string[] GetAssetFields(GraphQLField field)
            =>field.SelectionSet!.Selections.OfType<GraphQLField>().Select(x => x.Name.StringValue).ToArray();
        
        async Task<Result<GraphAttribute>> LoadArgs(GraphQLField field, GraphAttribute graphAttr)
        {
            if (!graphAttr.GetEntityLinkDesc().Try(out var desc, out var err))
                return Result.Fail(err);
            var inputs = field.Arguments?.Select(x => new GraphArgument(x)) ?? [];
            if (!QueryHelper.ParseSimpleArguments(inputs).Try( out var res, out  err)) 
                return Result.Fail(err);
            if (!(await res.Sorts.ToValidSorts(desc.TargetEntity, entitySchemaSvc,status)).Try(out var sorts, out err))
                return Result.Fail(err);
            if (!(await res.Filters.ToValidFilters(desc.TargetEntity,status, entitySchemaSvc, entitySchemaSvc)).Try(
                    out var filters, out err)) return Result.Fail(err);
            return graphAttr with { Pagination = res.Pagination, Filters = [..filters], Sorts = [..sorts] };
        }
        

        Task<Result<GraphAttribute>> LoadChildren(
            string newPrefix, GraphAttribute attr, GraphQLField field 
        ) => attr.GetEntityLinkDesc()
            .Bind(desc => ParseGraphFields(newPrefix, desc.TargetEntity, field.SelectionSet!.Selections.OfType<GraphQLField>(),attr,status, ct))
            .Map(sub => attr with { Selection = [..sub] });
    }
}