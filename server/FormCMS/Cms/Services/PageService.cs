using FormCMS.Utils.PageRender;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
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
    public async Task<string> Get(string name, StrArgs strArgs, string? nodeId, long? sourceId, Span? span, CancellationToken ct)
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
                return """ <a href="/admin">Go to Admin Panel</a><br/> <a href="/schema">Go to Schema Builder</a> """;
            }
            throw;
        }
        if (nodeId is not null)
        {
            return await RenderPartialPage(ctx.LoadPartialContext(nodeId), sourceId, span??new Span(), strArgs, ct);
        }
        return await RenderPage(ctx.LoadPageContext(), new Dictionary<string, object>(), strArgs, ct);
    }

    public async Task<string> GetDetail(string name, string slug, StrArgs strArgs, string? nodeId, long? sourceId,
        Span span, CancellationToken ct)
    {
        var ctx = await GetContext(name, true, strArgs, ct);
        if (nodeId is not null)
        {
            return await RenderPartialPage(ctx.LoadPartialContext(nodeId), sourceId, span, strArgs, ct);
        }

        var routerName = ctx.Page.Name.Split("/").Last()[1..^1]; // remove '{' and '}'
        strArgs[routerName] = slug;

        var pageCtx = ctx.LoadPageContext();
        foreach (var node in pageCtx.DateNodes.Where(x => string.IsNullOrWhiteSpace(x.Query)))
        {
            strArgs[node.Field + PaginationConstants.OffsetSuffix] = node.Offset.ToString();
            strArgs[node.Field + PaginationConstants.LimitSuffix] = node.Limit.ToString();
        }
        
        var data = string.IsNullOrWhiteSpace(ctx.Page.Query)
            ? new Dictionary<string, object>()
            : await querySvc.SingleWithAction(ctx.Page.Query, strArgs, ct)
              ?? throw new ResultException($"Could not data with {routerName} [{slug}]");

        return await RenderPage(pageCtx, data, strArgs, ct);
    }
    private async Task<string> RenderPartialPage(PartialContext ctx, long? sourceId, Span span, StrArgs args, CancellationToken ct)
    {
        Record[] items;
        var node = ctx.DataNodes.First();
        
        if (!string.IsNullOrWhiteSpace(node.Query))
        {
            var pagination = new Pagination(null, node.Limit.ToString());
            args = args.OverwrittenBy(QueryHelpers.ParseQuery(node.QueryString));
            items = await querySvc.ListWithAction(node.Query, span, pagination,args , ct);
        }
        else
        {
            items = await querySvc.Partial(ctx.Page.Query!, 
                node.Field,
                sourceId!.Value, 
                span, 
                node.Limit, 
                args,
                ct);
        }

        var data = new Dictionary<string, object> { [node.Field] = items };
        if (sourceId is not null)
        {
            data[QueryConstants.RecordId] = sourceId.Value;
        }

        foreach (var n in ctx.DataNodes)
        {
            SetMetadata(n.HtmlNode);
            n.HtmlNode.SetEach(node.Field);
        }
        return Handlebars.Compile(ctx.Node.InnerHtml)(data);
    }

    private async Task<string> RenderPage(PageContext ctx, Record data, StrArgs args, CancellationToken token)
    {
        foreach (var node in ctx.DateNodes.Where(x =>
                     //lazy query wait to load partial
                     !string.IsNullOrWhiteSpace(x.Query) && !x.Lazy))
        {
            var pagination = new Pagination(node.Offset.ToString(), node.Limit.ToString());
            var result = await querySvc.ListWithAction(
                node.Query,
                new Span(), pagination,
                args.OverwrittenBy(QueryHelpers.ParseQuery(node.QueryString)),
                token);
            data[node.Field] = result;
        }

        foreach (var dataNode in ctx.DateNodes)
        {
            SetMetadata(dataNode.HtmlNode);
            dataNode.HtmlNode.SetEach(dataNode.Field);
        }

        var title = Handlebars.Compile(ctx.Page.Title)(data);
        var body = Handlebars.Compile(ctx.HtmlDocument.DocumentNode.FirstChild.InnerHtml)(data);
        return template.Build(title, body, ctx.Page.Css);
    }

    private static void SetMetadata(HtmlNode node)
    {
        node.SetAttributeValue(QueryConstants.RecordId, $$$"""{{{{{QueryConstants.RecordId}}}}}""");
        
        var first = node.FirstChild;
        first.SetAttributeValue(QueryConstants.RecordId, $$$"""{{{{{QueryConstants.RecordId}}}}}""");
        first.SetAttributeValue(SpanConstants.Cursor, $$$"""{{{{{SpanConstants.Cursor}}}}}""");
        first.SetAttributeValue(SpanConstants.HasNextPage, $$$"""{{{{{SpanConstants.HasNextPage}}}}}""");
        first.SetAttributeValue(SpanConstants.HasPreviousPage, $$$"""{{{{{SpanConstants.HasPreviousPage}}}}}""");
    } 
    
    private record Context(Page Page, HtmlDocument Doc)
    {
        public PartialContext LoadPartialContext(string nodeId)
        {
            var htmlNode = Doc.GetElementbyId(nodeId);
            return new PartialContext(Page,htmlNode , htmlNode.GetDataNodesIncludeRoot().Ok());
        }

        public PageContext LoadPageContext() => new(Page, Doc, Doc.DocumentNode.GetDataNodes().Ok());
    }

    private record PageContext(Page Page, HtmlDocument HtmlDocument, DataNode[] DateNodes);

    private record PartialContext(Page Page, HtmlNode Node, DataNode[] DataNodes);

    private async Task<Context> GetContext(string name, bool matchPrefix, StrArgs args, CancellationToken ct)
    {
        var publicationStatus = PublicationStatusHelper.GetSchemaStatus(args);
        var schema = await pageResolver.GetPage(name, matchPrefix, publicationStatus, ct);

        var doc = new HtmlDocument();
        doc.LoadHtml(schema.Settings.Page!.Html);
        return new Context(schema.Settings.Page!, doc);
    }
}