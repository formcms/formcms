using Azure.Core;
using FormCMS.Activities.Services;
using FormCMS.Utils.PageRender;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.StrArgsExt;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Services;

public sealed class PageService(
    IQueryService querySvc,
    IPageResolver pageResolver,
    PageTemplate template
) : IPageService
{
    public async Task<string> GetDetail(string name, string param, StrArgs strArgs, CancellationToken ct)
    {
        var ctx = (await GetContext(name, true, strArgs, ct)).ToPageContext();
        strArgs = GetLocalPaginationArgs(ctx, strArgs);

        var routerName = ctx.Page.Name.Split("/").Last()[1..^1]; // remove '{' and '}'
        strArgs[routerName] = param;

        var data = string.IsNullOrWhiteSpace(ctx.Page.Query)
            ? new Dictionary<string, object>()
            : await querySvc.SingleWithAction(ctx.Page.Query, strArgs, ct)
              ?? throw new ResultException($"Could not data with {routerName} [{param}]");

        return await RenderPage(ctx, data, strArgs, ct);
    }

    public async Task<string> Get(string name, StrArgs strArgs, CancellationToken ct)
    {
        Context ctx;
        try
        {
            ctx = await GetContext(name, false, strArgs, ct);
        }
        catch
        {
            if (name == PageConstants.Home)
            {
                return """
                       <a href="/admin">Go to Admin Panel</a><br/>
                       <a href="/schema">Go to Schema Builder</a>
                       """;
            }
            throw ;
        }
        return await RenderPage(ctx.ToPageContext(), new Dictionary<string, object>(), strArgs, ct);
    }

    //
    public async Task<string> GetPart(long? sourceId,string partStr,bool replace, CancellationToken ct)
    {
        var part = PagePartHelper.Parse(partStr) ?? throw new ResultException("Invalid Partial Part");
        var cursor = new Span(part.First, part.Last);

        var args = QueryHelpers.ParseQuery(part.DataSource.QueryString);
        var ctx = (await GetContext(part.Page, false, args, ct)).ToPartialContext(part.NodeId);

        Record[] items;
        if (!string.IsNullOrWhiteSpace(part.DataSource.Query))
        {
            var pagination = new Pagination(null, part.DataSource.Limit.ToString());
            items = await querySvc.ListWithAction(part.DataSource.Query, cursor, pagination, args, ct);
        }
        else
        {
            items = await querySvc.Partial(ctx.Page.Query!, 
                part.DataSource.Field,
                sourceId!.Value, 
                cursor, 
                part.DataSource.Limit, 
                args,
                ct);
        }

        var flatField = RenderUtil.Flat(part.DataSource.Field);
        var data = new Dictionary<string, object>
        {
            [flatField] = items,
        };
        if (sourceId.HasValue)
        {
            data[RenderUtil.SourceIdTag(part.DataSource.Field)] = sourceId.Value;
        }
            
        TagPagination(data,items, part);

        ctx.Node.SetEach(flatField);
        ctx.Node.SetPagination(flatField, part.DataSource.PaginationMode);
        var html = replace ? ctx.Node.OuterHtml : ctx.Node.InnerHtml; 
        var render = Handlebars.Compile(html);
        return render(data);
    }

    private async Task<string> RenderPage(PageContext ctx, Record data, StrArgs args, CancellationToken token)
    {
        await LoadDataList(data, args, ctx.DateNodes, token);
        TagPagination(ctx, data, args);
        
        foreach (var (htmlNode, dataSource) in ctx.DateNodes)
        {
            htmlNode.SetEach(dataSource.Field);
            htmlNode.SetPagination(dataSource.Field, dataSource.PaginationMode);
        }
        
        var title = Handlebars.Compile(ctx.Page.Title)(data);
        var body = ctx.HtmlDocument.RenderBody(data);
        return template.Build(title, body, ctx.Page.Css);
    }

    private static StrArgs GetLocalPaginationArgs(PageContext ctx, StrArgs strArgs)
    {
        var ret = new StrArgs(strArgs);
        foreach (var node in ctx.DateNodes.Where(x =>
                     string.IsNullOrWhiteSpace(x.DataSource.Query) &&
                     (x.DataSource.Offset > 0 || x.DataSource.Limit > 0)))
        {
            ret[node.DataSource.Field + PaginationConstants.OffsetSuffix] = node.DataSource.Offset.ToString();
            ret[node.DataSource.Field + PaginationConstants.LimitSuffix] = node.DataSource.Limit.ToString();
        }

        return ret;
    }

    private async Task LoadDataList(Record data, StrArgs args, DataNode[] nodes, CancellationToken token)
    {
        foreach (var node in nodes.Where(x => !string.IsNullOrWhiteSpace(x.DataSource.Query)))
        {
            var pagination = new Pagination(node.DataSource.Offset.ToString(), node.DataSource.Limit.ToString());
            var result = await querySvc.ListWithAction(node.DataSource.Query, new Span(), pagination,
                node.MergeArgs(args), token);
            data[node.DataSource.Field] = result;
        }
    }
    
    private static void TagPagination(PageContext ctx, Record data, StrArgs args)
    {
        foreach (var node in ctx.DateNodes.Where(x => x.DataSource.Offset > 0 || x.DataSource.Limit > 0))
        {
            if (data.ByJsonPath<Record[]>(node.DataSource.Field, out var value) && value != null)
            {
                var nodeWithArg = node with
                {
                    DataSource = node.DataSource with { QueryString = node.MergeArgs(args).ToQueryString() }
                };
                TagPagination(data, value, nodeWithArg.ToPagePart(ctx.Page.Name));
                TagSourceId(data, value, nodeWithArg.ToPagePart(ctx.Page.Name));
            }
        }
    }


    private static void TagSourceId(Record data, Record[] items, PagePart token)
    {
        var field = token.DataSource.Field;
        var parts = field.Split('.');
        var sourceIdField = string.Join(".",[..parts[..^1],QueryConstants.RecordId]);
        if (data.ByJsonPath<long>(sourceIdField,out var sourceId))
        {
            data[RenderUtil.SourceIdTag(field)] = sourceId;
        }
    }
    private static void TagPagination(Record data, Record[] items, PagePart token)
    {
        var field = token.DataSource.Field;
        if (SpanHelper.HasPrevious(items))
        {
            data[RenderUtil.FirstAttrTag(field)] =
                PagePartHelper.ToString(token with { First = SpanHelper.FirstCursor(items), Last = "" });
        }

        if (SpanHelper.HasNext(items))
        {
            data[RenderUtil.LastAttrTag(field)] =
                PagePartHelper.ToString(token with { Last = SpanHelper.LastCursor(items), First = "" });
        }
        
        //use to reload first page
        data[RenderUtil.FirstPageTag(field)] = PagePartHelper.ToString(token with { Last = "", First = "" });

    }

    record Context(Page Page, HtmlDocument Doc)
    {
        public PartialContext ToPartialContext(string nodeId) => new(Page, Doc.GetElementbyId(nodeId));

        public PageContext ToPageContext() => new(Page, Doc, Doc.GetDataNodes().Ok());
    }

    private record PageContext(Page Page, HtmlDocument HtmlDocument, DataNode[] DateNodes);

    private record PartialContext(Page Page, HtmlNode Node);

    private async Task<Context> GetContext(string name, bool matchPrefix, StrArgs args, CancellationToken ct)
    {
        var publicationStatus = PublicationStatusHelper.GetSchemaStatus(args);
        var schema = await pageResolver.GetPage(name, matchPrefix, publicationStatus, ct);

        var doc = new HtmlDocument();
        doc.LoadHtml(schema.Settings.Page!.Html);
        return new Context(schema.Settings.Page!, doc);
    }
}

