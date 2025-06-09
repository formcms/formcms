using FormCMS.Cms.Graph;
using FormCMS.Core.Plugins;
using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using FormCMS.Utils.StrArgsExt;
using Humanizer;

namespace FormCMS.Cms.Services;

public sealed class QueryService(
    KateQueryExecutor executor,
    IQuerySchemaService schemaSvc,
    IEntitySchemaService resolver,
    IServiceProvider provider,
    HookRegistry hook,
    PluginRegistry  registry
) : IQueryService
{
    public async Task<Record[]> ListWithAction(GraphQlRequestDto dto)
        => await ListWithAction(await FromGraphQlRequest(dto, dto.Args), new Pagination(), new Span(), dto.Args);

    public async Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, StrArgs args,
        CancellationToken ct)
    {
        if (registry.PluginQueries.Contains(name))
        {
            var queryRes = await hook.BuildInQueryArgs.Trigger(provider, new BuildInQueryArgs(name, span, pagination, args));
            return queryRes.OutRecords??throw new ResultException($"Fail to get query result for [{name}]");
        }
        var query = await FromSavedQuery(name, args, ct);
        return await ListWithAction(query, pagination, span, args, ct);
    }

    public async Task<Record?> SingleWithAction(GraphQlRequestDto dto)
        => await SingleWithAction(await FromGraphQlRequest(dto, dto.Args), dto.Args);

    public async Task<Record?> SingleWithAction(string name, StrArgs args, CancellationToken ct)
        => await SingleWithAction(await FromSavedQuery(name, args, ct), args, ct);

    public async Task<Record[]> Partial(string name, string attr, long sourceId, Span span, int limit, StrArgs args,
        CancellationToken ct)
    {
        var runtimePagination = new Pagination(null, limit.ToString());
        var query = await FromSavedQuery(name, args, ct);
        if (registry.PluginAttributes.ContainsKey(name))
        {
            var addOnNode = query.Selection.First(x => x.Field == name);
            var queryPartialArgs = new QueryPartialArgs(query,addOnNode  ,span, sourceId);
            var res = await hook.QueryPartial.Trigger(provider, queryPartialArgs);
            return res.OutRecords ?? [];
        }

        var node = query.Selection.RecursiveFind(attr) ?? throw new ResultException("can not find attribute");
        var desc = node.LoadedAttribute!.GetEntityLinkDesc().Ok();

        var pagination = PaginationHelper.ToValid(
            runtimePagination, 
            node.QueryArgs.Pagination,
            desc.TargetEntity.DefaultPageSize, true, args);

        var fields = node.Selection
            .Where(x =>x is { IsNormalAttribute: true, LoadedAttribute: not null } && x.LoadedAttribute.DataType.IsLocal())
            .Select(x => x.LoadedAttribute!).ToArray()??[];
        var validSpan = span.ToValid(fields).Ok();

        var filters = FilterHelper.ReplaceVariables(node.Filters??[], args).Ok();
        var sorts = await SortHelper.ReplaceVariables(node.Sorts??[], args, desc.TargetEntity, resolver,
            PublicationStatusHelper.GetSchemaStatus(args)).Ok();

        var kateQuery = desc.GetQuery(fields,
            [new ValidValue(L: sourceId)],
            new CollectiveQueryArgs(filters, sorts, pagination.PlusLimitOne(), validSpan),
            PublicationStatusHelper.GetDataStatus(args)
        );

        var records = await executor.Many(kateQuery, ct);

        records = span.ToPage(records, pagination.Limit);

        if (records.Length <= 0) return records;

        SetRecordId(desc.TargetEntity, records);
        await LoadItems(node.Selection, args, records, ct);
        await LoadAsset([..node.Selection], records);
        new[] { node }.SetSpan(records, sorts);
        return records;
    }

    /*
     * why distinct:
     posts:[ {id:1,title:p1}]
     tags:[ {id:1, name:t1}, {id:2, name:t2} ]
     post_tag :[{post_id:1,tag_id:1},{post_id:1,tag_id:2}]

     select posts.id, posts.name from posts
     left join post_tags on posts.id = post_tag.post_id
     left join tags on post_tag.tag_id = tags.id
     where tags.id > 0;

     results: posts:[ {id:1,title:p1},{id:1,title:p1}]
     * limitation on distinct:
     for sql server, can not distinct on Text field

     soution/work around:
     create two query for an entity, one for list, one for detail(query by ID), only put Text field to Detail query
     */
    private async Task<Record[]> ListWithAction(LoadedQuery query, Pagination runtimePagination, Span span,
        StrArgs args, CancellationToken ct = default)
    {
        var pagination = PaginationHelper.ToValid(runtimePagination, query.Pagination, query.Entity.DefaultPageSize,
            !span.IsEmpty(), args);
        var validSpan = span.ToValid([..query.Entity.Attributes]).Ok();

        var hookParam = new QueryPreListArgs(query, query.Filters, query.Sorts, validSpan,
            pagination.PlusLimitOne());

        var res = await hook.QueryPreList.Trigger(provider, hookParam);

        Record[] items;
        if (res.OutRecords is not null)
        {
            items = span.ToPage(res.OutRecords, pagination.Limit);
        }
        else
        {
            var status = PublicationStatusHelper.GetDataStatus(args);
            var fields = query.Selection
                .Where(x =>x is { IsNormalAttribute: true, LoadedAttribute: not null } && x.LoadedAttribute.DataType.IsLocal())
                .Select(x=>x.LoadedAttribute!);
            var kateQuery = query.Entity.ListQuery([..query.Filters], [..query.Sorts], pagination.PlusLimitOne(),
                validSpan, fields, status);
            if (query.Distinct) kateQuery = kateQuery.Distinct();
            items = await executor.Many(kateQuery, ct);
            items = span.ToPage(items, pagination.Limit);
            if (items.Length > 0)
            {
                SetRecordId(query.Entity, items);
                await LoadItems(query.Selection, args, items, ct);
                await LoadAsset([..query.Selection], items);
            }
        }

        query.Selection.SetSpan(items, [..query.Sorts]);
        var postParam = new QueryPostListArgs(
            query,
            validSpan,
            pagination.PlusLimitOne(),
            items
        );
        postParam = await hook.QueryPostList.Trigger(provider, postParam);
        return postParam.RefRecords;
    }

    private async Task LoadAsset(GraphNode[] attributes, Record[] records)
    {
        var fields = attributes.GetAssetFields();
        if (fields.Length == 0) return;

        var paths = attributes.GetAllAssetPath(records);
        if (paths.Length == 0)
        {
            return;
        }

        if (fields.Length == 1 && fields[0] == nameof(Asset.Path).Camelize())
        {
            //no need to query asset table
            var assets = paths.Select(x => new Asset(x, "", "", "", 0, "", new Dictionary<string, object>(), ""));
            attributes.ReplaceAsset(records, assets.ToDictionary(x => x.Path));
        }
        else
        {
            if (!fields.Contains(nameof(Asset.Path).Camelize()))
            {
                fields = [..fields, nameof(Asset.Path).Camelize()];
            }

            var assetRecords = await executor.Many(Assets.GetAssetsByPaths(fields, paths));
            var assets = assetRecords
                .Select(x => x.ToObject<Asset>().Ok())
                .ToDictionary(x => x.Path);
            attributes.ReplaceAsset(records, assets);
        }
    }

    private async Task<Record?> SingleWithAction(LoadedQuery query, StrArgs args, CancellationToken ct = default)
    {
        var prePrams = new QueryPreSingleArgs(query);
        prePrams = await hook.QueryPreSingle.Trigger(provider, prePrams);

        Record? item;
        if (prePrams.OutRecord is not null)
        {
            item = prePrams.OutRecord;
        }
        else
        {
            var fields = query.Selection
                .Where(x =>x is { IsNormalAttribute: true, LoadedAttribute: not null } && x.LoadedAttribute.DataType.IsLocal())
                .Select(x=>x.LoadedAttribute!);
            PublicationStatus? pubStatus =
                args.ContainsEnumKey(SpecialQueryKeys.Preview) ? null : PublicationStatus.Published;
            var kateQuery = query.Entity.SingleQuery([..query.Filters], [..query.Sorts], fields, pubStatus).Ok();
            item = await executor.Single(kateQuery, ct);
            if (item is not null)
            {
                SetRecordId(query.Entity, [item]);
                await LoadItems(query.Selection, args, [item], ct);
                await LoadAsset([..query.Selection], [item]);
            }
        }

        if (item is null) return item;

        var postPram = new QueryPostSingleArgs(query, item);
        postPram = await hook.QueryPostSingle.Trigger(provider, postPram);
        item = postPram.RefRecord;
        
        query.Selection.SetSpan([item], []);
        return item;
    }

    private static void SetRecordId(LoadedEntity entity, Record[] records)
    {
        foreach (var record in records)
        {
            record[QueryConstants.RecordId] = record[entity.PrimaryKey];
        }
    }

    private async Task LoadItems(IEnumerable<GraphNode>? nodes, StrArgs args, Record[] items, CancellationToken ct)
    {
        if (nodes is null) return;

        foreach (var node in nodes)
        {
            if (node is not { IsNormalAttribute: true, LoadedAttribute: not null }) continue;
            var attr = node.LoadedAttribute;
            if (attr.DataType.IsCompound())
            {
                await AttachRelated(node, args, items, ct);
            }
            else
            {
                attr.FormatForDisplay(items);
            }
        }
    }

    private async Task AttachRelated(GraphNode node, StrArgs args, Record[] items, CancellationToken ct)
    {
        var attr = node.LoadedAttribute;
        if (attr is null) return;
        
        var desc = attr.GetEntityLinkDesc().Ok();
        var ids = desc.SourceAttribute.GetUniq(items);
        if (ids.Length == 0) return;

        CollectiveQueryArgs? collectionArgs = null;
        if (desc.IsCollective)
        {
            var filters = FilterHelper.ReplaceVariables(node.Filters??[], args).Ok();
            var sorts = await SortHelper.ReplaceVariables(node.Sorts??[], args, desc.TargetEntity, resolver,
                PublicationStatusHelper.GetSchemaStatus(args)).Ok();
            var fly = PaginationHelper.ResolvePagination(node, args) ?? new Pagination();
            var validPagination = fly.IsEmpty()
                ? null
                : PaginationHelper.ToValid(fly, node.QueryArgs.Pagination, desc.TargetEntity.DefaultPageSize, false, args);
            collectionArgs = new CollectiveQueryArgs(filters, sorts, validPagination, null);
        }

        if (collectionArgs?.Pagination is null)
        {
            //get all items and no pagination
            var attrs = node.Selection
                .Where(x => x is { IsNormalAttribute: true} && x.LoadedAttribute.DataType.IsLocal())
                .Select(x => x.LoadedAttribute!) ?? [];
            
            var query = desc.GetQuery(attrs, ids, collectionArgs,
                PublicationStatusHelper.GetDataStatus(args));
            var targetRecords = await executor.Many(query, ct);

            if (targetRecords.Length > 0)
            {
                var groups = targetRecords.GroupBy(x => desc.TargetAttribute.GetValueOrLookup(x), x => x);
                foreach (var group in groups)
                {
                    var sourceItems = items.Where(x => x[desc.SourceAttribute.Field].Equals(group.Key));
                    object? targetValues = desc.IsCollective ? group.ToArray() : group.FirstOrDefault();
                    if (targetValues is null) continue;
                    foreach (var item in sourceItems)
                    {
                        item[attr.Field] = targetValues;
                    }
                }

                SetRecordId(desc.TargetEntity, targetRecords);
                await LoadItems(node.Selection, args, targetRecords, ct);
            }
        }
        else
        {
            var fields = node.Selection
                .Where(x =>x is { IsNormalAttribute: true, LoadedAttribute: not null } && x.LoadedAttribute.DataType.IsLocal()).ToArray()
                .Select(x=>x.LoadedAttribute!).ToArray()??[];
            var pubStatus = PublicationStatusHelper.GetDataStatus(args);
            var plusOneArgs = collectionArgs with { Pagination = collectionArgs.Pagination.PlusLimitOne() };
            foreach (var id in ids)
            {

                var query = desc.GetQuery(fields, ids, plusOneArgs, pubStatus);
                var targetRecords = await executor.Many(query, ct);

                targetRecords = new Span().ToPage(targetRecords, collectionArgs.Pagination.Limit);
                if (targetRecords.Length > 0)
                {
                    var sourceItems = items.Where(x => x[desc.SourceAttribute.Field].Equals(id.ObjectValue));
                    foreach (var item in sourceItems)
                    {
                        item[attr.Field] = targetRecords;
                    }

                    SetRecordId(desc.TargetEntity, targetRecords);
                    await LoadItems(node.Selection, args, targetRecords, ct);
                }
            }
        }
    }

    private async Task<LoadedQuery> FromSavedQuery(
        string name, StrArgs args, CancellationToken token = default)
    {
        var status = PublicationStatusHelper.GetSchemaStatus(args);
        var query = await schemaSvc.GetSetCacheByName(name, status, token);
        query.VerifyVariable(args);
        if (status != PublicationStatus.Published)
        {
            //remove preview variables
            foreach (var (key, value) in args)
            {
                if ("{" + key + "}" == value)
                {
                    args.Remove(key);
                }
            }
        }
        return await ReplaceVariables(query, args, token);
    }

    private async Task<LoadedQuery> FromGraphQlRequest(GraphQlRequestDto dto, StrArgs args)
    {
        var query = await schemaSvc.ByGraphQlRequest(dto.Query, dto.Fields);
        return await ReplaceVariables(query, args);
    }

    private async Task<LoadedQuery> ReplaceVariables(LoadedQuery query, StrArgs args, CancellationToken token = default)
    {
        var sort = await SortHelper.ReplaceVariables(query.Sorts, args, query.Entity, resolver,
            PublicationStatusHelper.GetSchemaStatus(args)).Ok();
        var filters = FilterHelper.ReplaceVariables(query.Filters, args).Ok();

        var nodes = query.Selection.Select(node => node with
        {
            QueryArgs = node.QueryArgs with
            {
                Pagination = PaginationHelper.ResolvePagination(node, args) ?? node.QueryArgs.Pagination
            }
        });
        return query with { Sorts = [..sort], Filters = [..filters], Selection = [..nodes] };
    }
}