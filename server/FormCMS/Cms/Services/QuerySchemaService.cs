using FormCMS.Infrastructure.Cache;
using FormCMS.Cms.Graph;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Plugins;
using FormCMS.Utils.ResultExt;
using GraphQLParser.AST;
using Converter = FormCMS.Utils.GraphTypeConverter.Converter;
using Query = FormCMS.Core.Descriptors.Query;
using Schema = FormCMS.Core.Descriptors.Schema;

namespace FormCMS.Cms.Services;

public sealed class QuerySchemaService(
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    KeyValueCache<LoadedQuery> queryCache,
    PluginRegistry registry,
    SystemSettings systemSettings
) : IQuerySchemaService
{
    public async Task<LoadedQuery> ByGraphQlRequest(Query query, GraphQLField[] fields)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
        {
            return await ToLoadedQuery(query, fields, null);
        }

        var schema = await schemaSvc.GetByNameDefault(query.Name, SchemaType.Query, null, CancellationToken.None);
        if (schema == null || schema.Settings.Query != null && schema.Settings.Query.Source != query.Source)
        {
            await SaveQuery(query, null);
        }

        return await ToLoadedQuery(query, fields, null);
    }

    public async Task<LoadedQuery> GetSetCacheByName(string name, PublicationStatus? status,
        CancellationToken ct = default)
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


    public async Task SaveQuery(Query query, PublicationStatus? status, CancellationToken ct = default)
    {
        query = query with
        {
            IdeUrl =
            $"{systemSettings.GraphQlPath}?query={Uri.EscapeDataString(query.Source)}"
        };
        if (!registry.PluginEntities.ContainsKey(query.EntityName))
        {
            await VerifyQuery(query, status, ct);
        }

        var schema = new Schema(query.Name, SchemaType.Query, new Settings(Query: query));
        await schemaSvc.AddOrUpdateByNameWithAction(schema, false, ct);
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
        GraphQLField[] fields,
        PublicationStatus? status,
        CancellationToken ct = default)
    {
        var selection = ParseGraphNodes(fields, "");
        var entity = registry.PluginEntities.TryGetValue(query.EntityName, out var pluginEntity)
            ? pluginEntity.ToLoadedEntity()
            : await entitySchemaSvc.LoadEntity(query.EntityName, status, ct).Ok();
        
        selection = await LoadAttributes(selection, entity, null, status, ct);
        var sorts = await query.Sorts.ToValidSorts(entity, entitySchemaSvc, status).Ok();
        var validFilter = await query.Filters.ToValidFilters(entity, status, entitySchemaSvc).Ok();
        return query.ToLoadedQuery(entity, selection, sorts, validFilter);
    }

    private async Task VerifyQuery(Query? query, PublicationStatus? status, CancellationToken ct = default)
    {
        if (query is null)
        {
            throw new ResultException("query is null");
        }

        var entity = await entitySchemaSvc.LoadEntity(query.EntityName, status, ct).Ok();
        await query.Filters.ToValidFilters(entity, status, entitySchemaSvc).Ok();

        var fields = Converter.GetRootGraphQlFields(query.Source).Ok();
        var nodes = ParseGraphNodes(fields, "");
        await LoadAttributes(nodes, entity, null, status, ct);
        await query.Sorts.ToValidSorts(entity, entitySchemaSvc, status).Ok();
    }

    private static GraphNode[] ParseGraphNodes(IEnumerable<GraphQLField> fields, string prefix)
    {
        var ret = new List<GraphNode>();

        foreach (var field in fields)
        {
            var fieldName = field.Name.StringValue;
            var arguments = field.Arguments?.Select(x => new GraphArgument(x)) ?? [];
            var queryArgs = QueryHelper.ParseSimpleArguments(arguments).Ok();
            var node = new GraphNode(
                Field: fieldName, 
                Sorts:[..queryArgs.Sorts],
                Pagination:queryArgs.Pagination,
                Filters:[..queryArgs.Filters],
                Prefix: prefix, 
                Selection: [],
                LoadedAttribute: new LoadedAttribute("", fieldName),
                ValidFilters:[],
                ValidSorts:[]
                );
            if (field.SelectionSet is not null)
            {
                var newPrefix = string.IsNullOrWhiteSpace(prefix)
                    ? fieldName
                    : prefix + "." + fieldName;
                node = node with
                {
                    Selection = [..ParseGraphNodes(field.SelectionSet!.Selections.OfType<GraphQLField>(), newPrefix)]
                };
            }

            ret.Add(node);
        }

        return ret.ToArray();
    }

    private async Task<GraphNode[]> LoadAttributes(
        GraphNode[] nodes, LoadedEntity entity, GraphNode? parent, 
        PublicationStatus? status, CancellationToken ct)
    {
        var ret = new List<GraphNode>();
        foreach (var node in nodes)
        {
            var n = await LoadAttr(node);
            ret.Add(n);
        }
        return ret.ToArray();

        async Task<GraphNode> LoadAttr(GraphNode node)
        {
            var attr = await entitySchemaSvc.LoadSingleAttrByName(entity, node.Field, status, ct).Ok();

            if (nodes.FirstOrDefault(f => f.Field == entity.PrimaryKey) is null)
                throw new ResultException(
                    $"Primary key [{entity.PrimaryKey}] not in selection list for entity [{entity.Name}]");

            if (parent?.LoadedAttribute?.DataType == DataType.Collection &&
                parent.LoadedAttribute.GetEntityLinkDesc().Value.TargetAttribute.Field is { } field &&
                nodes.FirstOrDefault(attribute => attribute.Field == field) is null)
                throw new ResultException(
                    $"Referencing Field [{field}] not in selection list for entity [{entity.Name}]");

            var newNode = node with
            {
                IsNormalAttribute = !registry.PluginAttributes.ContainsKey(node.Field)
                                    && !registry.PluginEntities.ContainsKey(entity.Name)
                                    && (parent is null || parent.IsNormalAttribute),
                LoadedAttribute = attr,
            };

            if (!attr.DataType.IsCompound()) return newNode;
            var desc = attr.GetEntityLinkDesc().Ok();
            newNode = newNode with
            {
                ValidSorts =
                [
                    ..await node.Sorts.ToValidSorts(desc.TargetEntity, entitySchemaSvc, status).Ok()
                ],
                ValidFilters =
                [
                    ..await node.Filters.ToValidFilters(desc.TargetEntity, status, entitySchemaSvc).Ok()
                ],
                Selection = [..await LoadAttributes([..node.Selection], desc.TargetEntity, newNode, status, ct)]
            };
            return newNode;
        }
    }
}