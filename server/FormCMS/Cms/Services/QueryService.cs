using FormCMS.Cms.Graph;
using FormCMS.Core.Plugins;
using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
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
    PluginRegistry  registry,
    IUserManageService userManageService
) : IQueryService
{
    public async Task<Record[]> ListWithAction(GraphQlRequestDto dto)
        => await ListWithAction(await schemaSvc.ByGraphQlRequest(dto.Query,dto.Fields), new Pagination(), new Span(), dto.Args);

    public async Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, StrArgs args,
        CancellationToken ct)
    {
        if (registry.PluginQueries.Contains(name))
        {
            var queryRes = await hook.PlugInQueryArgs.Trigger(provider, new PlugInQueryArgs(name, span, pagination, args));
            return queryRes.OutRecords??throw new ResultException($"Fail to get query result for [{name}]");
        }
        var query = await FromSavedQuery(name, args, ct);
        return await ListWithAction(query, pagination, span, args, ct);
    }

    public async Task<Record?> SingleWithAction(GraphQlRequestDto dto)
        => await SingleWithAction(await schemaSvc.ByGraphQlRequest(dto.Query, dto.Fields), dto.Args);

    public async Task<Record?> SingleWithAction(string name, StrArgs args, CancellationToken ct)
        => await SingleWithAction(await FromSavedQuery(name, args, ct), args, ct);

    public async Task<Record[]> Partial(string name, string attr, long sourceId, Span span, int limit, StrArgs args,
        CancellationToken ct)
    {
        var query = await FromSavedQuery(name, args, ct);
        
        Record[] records;
        var  node = query.Selection.RecursiveFind(attr) ?? throw new ResultException("can not find attribute");
        var desc = node.LoadedAttribute.GetEntityLinkDesc().Ok();
        
        //pagination
        var variablePagination = new Pagination(null, limit.ToString());
        var pagination = PaginationHelper.MergeLimit(
            variablePagination,
            node.Pagination,
            args,
            desc.TargetEntity.DefaultPageSize
        );

        //span
        var attrs = node.Selection
            .Select(x => x.LoadedAttribute)
            .Where(x => x.DataType.IsLocal())
            .ToArray();
        var validSpan = span.ToValid(attrs).Ok();
        node = node with
        {
            ValidFilters = [..FilterHelper.ReplaceVariables(node.ValidFilters, args).Ok()],
            ValidSorts =
            [
                .. await SortHelper.ReplaceVariables(node.ValidSorts, args, desc.TargetEntity, resolver,
                    PublicationStatusHelper.GetSchemaStatus(args)).Ok()
            ],
        };
        
        
        if (registry.PluginAttributes.ContainsKey(attr))
        {
            var queryPartialArgs = new QueryPartialArgs(
                ParentEntity: query.Entity,
                Node: node,
                Span: validSpan,
                Pagination: pagination.PlusLimitOne(),
                SourceId: sourceId
            );
            var res = await hook.QueryPartial.Trigger(provider, queryPartialArgs);
            records = res.OutRecords ?? [];
        }
        else
        {
            var normalNodes = node.Selection
                .Where(x => x is { IsNormalAttribute: true })
                .ToArray();
            var normalAttrs = normalNodes
                .Where(x=> x.LoadedAttribute.DataType.IsLocal())
                .Select(x => x.LoadedAttribute).ToArray();

            var kateQuery = desc.GetQuery(normalAttrs,
                [new ValidValue(L: sourceId)],
                new CollectiveQueryArgs([..node.ValidFilters], [..node.ValidSorts], pagination.PlusLimitOne(), validSpan),
                PublicationStatusHelper.GetDataStatus(args)
            );

            records = await executor.Many(kateQuery, ct);
            await LoadSubNodeData(normalNodes, args, records, ct);
        }

        var postPartialArgs = new QueryPostPartialArgs(node, records);
        var postRes = await hook.QueryPostPartial.Trigger(provider, postPartialArgs);
        records = postRes.RefRecords;
        
        records = span.ToPage(records, pagination.Limit);
        if (records.Length <= 0) return records;

        SetSpan([..node.Selection],  records, node.ValidSorts.Select(x => x.Field).ToArray());
        await LoadDependency(desc.TargetEntity, [..node.Selection], records);
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
    private async Task<Record[]> ListWithAction(LoadedQuery query, Pagination variablePagination, Span span,
        StrArgs args, CancellationToken ct = default)
    {
        var pagination = span.IsEmpty()
            ? PaginationHelper.MergePagination(variablePagination, query.Pagination, args, query.Entity.DefaultPageSize)
            : PaginationHelper.MergeLimit(variablePagination, query.Pagination, args, query.Entity.DefaultPageSize);
        
        var validSpan = span.ToValid([..query.Entity.Attributes]).Ok();

        var sort = await SortHelper.ReplaceVariables(query.Sorts, args, query.Entity, resolver,
            PublicationStatusHelper.GetSchemaStatus(args)).Ok();
        
        var filters = FilterHelper.ReplaceVariables(query.Filters, args).Ok();
        
        query = query with { Sorts = [..sort], Filters = [..filters]};
        
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
            var normalNodes = query.Selection
                .Where(x => x is { IsNormalAttribute: true })
                .ToArray();
            var normalAttrs = normalNodes
                .Where(x=> x.LoadedAttribute.DataType.IsLocal())
                .Select(x => x.LoadedAttribute).ToArray();
           
            var kateQuery = query.Entity.ListQuery([..query.Filters], [..query.Sorts], pagination.PlusLimitOne(),
                validSpan, normalAttrs, status);
            if (query.Distinct) kateQuery = kateQuery.Distinct();
            items = await executor.Many(kateQuery, ct);
            items = span.ToPage(items, pagination.Limit);
            if (items.Length > 0)
            {
                await LoadSubNodeData(normalNodes, args, items, ct);
            }
        }

        var postParam = new QueryPostListArgs(
            query,
            validSpan,
            pagination.PlusLimitOne(),
            items
        );
        postParam = await hook.QueryPostList.Trigger(provider, postParam);
        var records = postParam.RefRecords;
        SetSpan([..query.Selection],records, [..query.Sorts.Select(x=>x.Field).ToArray()]);
        await LoadDependency(query.Entity, [..query.Selection], records);
        return postParam.RefRecords;
    }

    private async Task<Record?> SingleWithAction(LoadedQuery query, StrArgs args, CancellationToken ct = default)
    {
        var filters = FilterHelper.ReplaceVariables(query.Filters, args).Ok();
       query = query with {  Filters = [..filters]};
        var prePrams = new QueryPreSingleArgs(query);
        
        prePrams = await hook.QueryPreSingle.Trigger(provider, prePrams);

        Record? item;
        if (prePrams.OutRecord is not null)
        {
            item = prePrams.OutRecord;
        }
        else
        {
            var normalNodes = query.Selection
                .Where(x => x is { IsNormalAttribute: true })
                .ToArray();
            var normalAttrs = normalNodes
                .Where(x=> x.LoadedAttribute.DataType.IsLocal())
                .Select(x => x.LoadedAttribute).ToArray(); 
            
            PublicationStatus? pubStatus =
                args.ContainsEnumKey(SpecialQueryKeys.Preview) ? null : PublicationStatus.Published;
            var kateQuery = query.Entity.SingleQuery([..query.Filters], [..query.Sorts], normalAttrs, pubStatus).Ok();
            item = await executor.Single(kateQuery, ct);
            if (item is not null)
            {
                await LoadSubNodeData(normalNodes, args, [item], ct);
            }
        }

        if (item is null) return item;

        var postPram = new QueryPostSingleArgs(query, item, args);
        postPram = await hook.QueryPostSingle.Trigger(provider, postPram);
        item = postPram.RefRecord;

        SetSpan([..query.Selection],[item], []);
        await LoadDependency(query.Entity, [..query.Selection], [item]);
        return item;
    }
    
    private async Task LoadSubNodeData(IEnumerable<GraphNode> nodes, StrArgs args, Record[] records, CancellationToken ct)
    {
        foreach (var graphNode in nodes)
        {
            if (graphNode.LoadedAttribute.DataType.IsCompound())
            {
                await LoadOneSubNodeData(graphNode, args, records, ct);
            }
        }
    }
    
    private async Task LoadOneSubNodeData(GraphNode node, StrArgs args, Record[] items, CancellationToken ct)
    {
        if (!node.IsNormalAttribute) return;
        
        var desc = node.LoadedAttribute.GetEntityLinkDesc().Ok();
        var ids = desc.SourceAttribute.GetUniq(items);
        if (ids.Length == 0) return;

        CollectiveQueryArgs? collectionArgs = null;
        if (desc.IsCollective)
        {
            var filters = FilterHelper.ReplaceVariables(node.ValidFilters, args).Ok();
            var sorts = await SortHelper.ReplaceVariables(node.ValidSorts, args, desc.TargetEntity, resolver,
                PublicationStatusHelper.GetSchemaStatus(args)).Ok();
            var variablePagination = PaginationHelper.FromVariables(args, node.Prefix, node.Field);
            var validPagination = variablePagination.IsEmpty() && node.Pagination.IsEmpty()
                ? null
                : PaginationHelper.MergePagination(variablePagination, node.Pagination,args, desc.TargetEntity.DefaultPageSize);
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
                        item[node.LoadedAttribute.Field] = targetValues;
                    }
                }

                await LoadSubNodeData(node.Selection, args, targetRecords, ct);
            }
        }
        else
        {
            var fields = node.Selection
                .Where(x =>x is { IsNormalAttribute: true} && x.LoadedAttribute.DataType.IsLocal()).ToArray()
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
                        item[node.LoadedAttribute.Field] = targetRecords;
                    }

                    await LoadSubNodeData(node.Selection, args, targetRecords, ct);
                }
            }
        }
    }

    private static void SetSpan(GraphNode[] nodes, Record[] items, string[] sortsFields)
    {
        if (SpanHelper.HasPrevious(items)) SpanHelper.SetCursor(items.First(), sortsFields);
        if (SpanHelper.HasNext(items)) SpanHelper.SetCursor(items.Last(), sortsFields);

        foreach (var node in nodes)
        {
            if (node.LoadedAttribute.DataType.IsCompound())
            {
                foreach (var item in items)
                {
                    if (item.TryGetValue(node.Field, out var v) && v is not null)
                    {
                        if (v is Record record)
                        {
                            SetSpan([..node.Selection], [record], []);
                        }
                        else if (v is Record[] records)
                        {
                            SetSpan([..node.Selection], records, node.ValidSorts.Select(x => x.Field).ToArray());
                        }
                    }
                }
            }
        }
    }

    private async Task LoadDependency(LoadedEntity entity, GraphNode[] nodes, Record[] records)
    {
        SetRecordId(entity, nodes, records);
        FormatForDisplay(entity, nodes,records);
        await LoadPublicUser(entity,nodes,records);
        await LoadAssets(entity, nodes, records);       
    }

    private async Task LoadAssets(LoadedEntity entity, GraphNode[] nodes, Record[] records)
    {
        var paths = GetAllAssetPaths();
        var assetRecords = await executor.Many(Assets.GetAssetsByPaths(paths));
        var assets = assetRecords.ToDictionary(x => x.StrOrEmpty(nameof(Asset.Path).Camelize()));
        ReplaceAssets();
        return;

        void ReplaceAssets()
        {
            nodes.Iterate(entity, records,
                singleAction: (_, node, record) =>
                {
                    switch (node.LoadedAttribute.DisplayType)
                    {
                        case DisplayType.File or DisplayType.Image:
                        {
                            if (record.TryGetValue(node.Field, out var val) && val is not null)
                            {
                                record[node.Field] = val is string str && assets.TryGetValue(str, out var asset)
                                    ? asset
                                    : null!;
                            }

                            break;
                        }
                        case DisplayType.Gallery:
                        {
                            if (record.TryGetValue(node.Field, out var val) && val is string[] arr)
                            {
                                var list = new List<Record>();
                                foreach (var se in arr)
                                {
                                    if (assets.TryGetValue(se, out var asset))
                                    {
                                        list.Add(asset);
                                    }
                                }

                                record[node.Field] = list.ToArray();
                            }

                            break;
                        }
                    }
                });
        }

        string[] GetAllAssetPaths()
        {
            var ret = new HashSet<string>();
            nodes.Iterate(entity, records, singleAction: (_, node, rec) =>
            {
                switch (node.LoadedAttribute.DisplayType)
                {
                    case DisplayType.File or DisplayType.Image:
                    {
                        if (rec.TryGetValue(node.Field, out var val) && val is string s)
                        {
                            ret.Add(s);
                        }

                        break;
                    }
                    case DisplayType.Gallery:
                    {
                        if (rec.TryGetValue(node.Field, out var val) && val is string[] arr)
                            foreach (var se in arr)
                            {
                                ret.Add(se);
                            }

                        break;
                    }
                }
            });
            return ret.ToArray();
        }
    }

    private async Task LoadPublicUser(LoadedEntity entity, GraphNode[] nodes, Record[] records)
    {
        var userIds = new HashSet<string>();
        nodes.Iterate(entity, records, singleAction: (_, node, record) =>
        {
            if (node.LoadedAttribute.DataType is DataType.Lookup
                && node.LoadedAttribute.GetLookupTarget(out var target)
                && target == PublicUserInfos.Entity.Name 
                && record.TryGetValue(node.Field, out var value) 
                && value is not null
                )
            {
                userIds.Add((string)value);
            }
        });
        var users = await userManageService.GetPublicUserInfos(userIds,CancellationToken.None);
        //make field camel case
        var userRecs = users.Select(x => RecordExtensions.FormObject(x));
        var dict = userRecs.ToDictionary(x => x.StrOrEmpty(nameof(PublicUserInfo.Id).Camelize()));
        
        nodes.Iterate(entity, records, singleAction: (_, node, record) =>
        {
            if (node.LoadedAttribute.DataType is DataType.Lookup
                && node.LoadedAttribute.GetLookupTarget(out var target)
                && target == PublicUserInfos.Entity.Name
                && record.TryGetValue(node.Field, out var value) 
                && value is string s
                )
            {
                record[node.Field] = dict.TryGetValue(s, out var user) ? user : null!;
            }
        });
    }
    private static void SetRecordId(LoadedEntity entity, GraphNode[] nodes, Record[] records)
    {
        nodes.Iterate(entity,records,singleAction:(en,_, record) =>
        {
            record[QueryConstants.RecordId] = record[en.PrimaryKey];
        });
    }
    
    private static void FormatForDisplay(LoadedEntity entity, GraphNode[] nodes, Record[] records)
    {
        nodes.Iterate(entity,records,singleAction:(en,node, record) =>
        {
            node.LoadedAttribute.FormatForDisplay(record);
        });
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
        return query;
    }
}