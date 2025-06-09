using FormCMS.Infrastructure.Cache;
using FormCMS.Cms.Graph;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Plugins;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using GraphQLParser.AST;
using Attribute = FormCMS.Core.Descriptors.Attribute;
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
        await VerifyQuery(query, status, ct);
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
        var entity = await entitySchemaSvc.LoadEntity(query.EntityName, status, ct).Ok();
        var selection = ParseGraphNodes(fields, "");
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
                QueryArgs: queryArgs, 
                Prefix: prefix, 
                Selection: [],
                LoadedAttribute: new LoadedAttribute("", fieldName));
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
            var n = registry.PluginAttributes.TryGetValue(node.Field, out var pluginAttr)
                    ? await LoadPluginAttr(node, pluginAttr)
                    : parent is not null && !parent.IsNormalAttribute
                        ? await LoadPluginChildren(node)
                        : await LoadNormalAttr(node);
            ret.Add(n);
        }

        return ret.ToArray();

        async Task<GraphNode> LoadNormalAttr(GraphNode node)
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
                IsNormalAttribute = true,
                LoadedAttribute = attr,
                AssetFields = attr.DisplayType.IsAsset()
                    ? [..node.Selection.Select(x => x.Field)]
                    : [],
            };

            if (!attr.DataType.IsCompound()) return newNode;
            var desc = attr.GetEntityLinkDesc().Ok();
            newNode = newNode with
            {
                Sorts =
                [
                    ..await node.QueryArgs.Sorts.ToValidSorts(desc.TargetEntity, entitySchemaSvc, status).Ok()
                ],
                Filters =
                [
                    ..await node.QueryArgs.Filters.ToValidFilters(desc.TargetEntity, status, entitySchemaSvc)
                        .Ok()
                ],
                Selection = [..await LoadAttributes([..node.Selection], desc.TargetEntity, newNode, status, ct)]
            };
            return newNode;
        }

        Task<GraphNode> LoadPluginChildren(GraphNode node)
        {
            var attr = entity.Attributes.First(x => x.Field == node.Field);
            return LoadPluginAttr(node, attr);
        }
        
        async Task<GraphNode> LoadPluginAttr(GraphNode node, Attribute pluginAttr)
        {
            var loadedAttribute = PlugInAttributeToLoaded(pluginAttr);
            var newPluginNode = node with { LoadedAttribute = loadedAttribute };
            if (!loadedAttribute.DataType.IsCompound()) return newPluginNode;
            var desc = loadedAttribute.GetEntityLinkDesc().Ok();
            newPluginNode = newPluginNode with
            {
                Selection = [..await LoadAttributes([..node.Selection], desc.TargetEntity, newPluginNode, status, ct)]
            };
            return newPluginNode;
        }

        LoadedAttribute PlugInAttributeToLoaded(Attribute attribute)
        {
            var loadedAttribute = attribute.ToLoaded("");
            if (attribute.DataType == DataType.Collection
                && attribute.GetCollectionTarget(out var target, out var linkAttrName)
                && registry.PluginEntities.TryGetValue(target, out var targetEntity))
            {
                var linkAttr = new LoadedAttribute("", linkAttrName);
                var collection = new Collection(entity, targetEntity.ToLoadedEntity(), linkAttr);
                loadedAttribute = loadedAttribute with { Collection = collection };
            }else if (attribute.DataType == DataType.Lookup 
                      && attribute.GetLookupTarget(out var lookupTarget)
                      && registry.PluginEntities.TryGetValue(lookupTarget, out var lookupEntity))
            {
                loadedAttribute = loadedAttribute with { Lookup = new Lookup(lookupEntity.ToLoadedEntity()) };
            }
            return loadedAttribute;
        }
    }
    
    
}