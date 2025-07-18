using FormCMS.Utils.PageRender;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
using FormCMS.Utils.StrArgsExt;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Services;

public sealed class PageService(
    SystemSettings systemSettings,
    IQueryService querySvc,
    IPageResolver pageResolver,
    PageTemplate template
) : IPageService
{
    public async Task<string> Get(string name, StrArgs strArgs, string? nodeId, long? sourceId, Span? span,
        CancellationToken ct)
    {
        PageProcessingContext ctx;
        try
        {
            ctx = await LoadPage(name, false, strArgs, ct);
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
            return await RenderPartialPage(ctx.LoadPartialContext(nodeId), sourceId, span ?? new Span(), strArgs, ct);
        }

        var pageCtx = ctx.ParseDataNodes();

        var data = new Dictionary<string, object>();
        await LoadData(pageCtx, strArgs, data, ct);
        return RenderPage(pageCtx, data, ct);
    }

    public async Task<string> GetDetail(string name, string slug, StrArgs strArgs, string? nodeId, long? sourceId,
        Span span, CancellationToken ct)
    {
        var ctx = await LoadPage(name, true, strArgs, ct);
        if (nodeId is not null)
        {
            return await RenderPartialPage(ctx.LoadPartialContext(nodeId), sourceId, span, strArgs, ct);
        }

        var routerName = ctx.CurrentPage.Name.Split("/").Last()[1..^1]; // remove '{' and '}'
        strArgs[routerName] = slug;

        var pageCtx = ctx.ParseDataNodes();
        foreach (var node in pageCtx.DataNodes.Where(x => string.IsNullOrWhiteSpace(x.Query)))
        {
            strArgs[node.Field + PaginationConstants.OffsetSuffix] = node.Offset.ToString();
            strArgs[node.Field + PaginationConstants.LimitSuffix] = node.Limit.ToString();
        }

        Record data;
        try
        {
            data = string.IsNullOrWhiteSpace(ctx.CurrentPage.Query)
                ? new Dictionary<string, object>()
                : await querySvc.SingleWithAction(ctx.CurrentPage.Query, strArgs, ct) ??
                  throw new ResultException($"Could not data with {routerName} [{slug}]");
        }
        catch (Exception e)
        {
            if (e is ResultException { Code: ErrorCodes.NOT_ENOUGH_ACCESS_LEVEL })
            {
                return template.BuildSubsPage(systemSettings.PortalRoot + "/sub/view");
            }
            throw;
        }
        await LoadData(pageCtx, strArgs, data, ct);
        return RenderPage(pageCtx, data, ct);
    }

    private async Task<string> RenderPartialPage(PartialPageContext ctx, long? sourceId, Span span, StrArgs args,
        CancellationToken ct)
    {
        Record[] items;
        var node = ctx.DataNodes.First();

        if (!string.IsNullOrWhiteSpace(node.Query))
        {
            var pagination = new Pagination(null, node.Limit.ToString());
            args = args.OverwrittenBy(QueryHelpers.ParseQuery(node.QueryString));
            items = await querySvc.ListWithAction(node.Query, span, pagination, args, ct);
        }
        else
        {
            items = await querySvc.Partial(ctx.CurrentPage.Query!,
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

        return Handlebars.Compile(ctx.Element.InnerHtml)(data);
    }

    private async Task LoadData(FullPageContext ctx, StrArgs args, Record data, CancellationToken ct)
    {
        foreach (var node in ctx.DataNodes.Where(x =>
                     //lazy query wait to load partial
                     !string.IsNullOrWhiteSpace(x.Query) && !x.Lazy))
        {
            var pagination = new Pagination(node.Offset.ToString(), node.Limit.ToString());
            var result = await querySvc.ListWithAction(
                node.Query,
                new Span(), pagination,
                args.OverwrittenBy(QueryHelpers.ParseQuery(node.QueryString)),
                ct);
            data[node.Field] = result;
        }
    }

    private string RenderPage(FullPageContext ctx, Record data, CancellationToken ct)
    {
        foreach (var dataNode in ctx.DataNodes)
        {
            SetMetadata(dataNode.HtmlNode);
            dataNode.HtmlNode.SetEach(dataNode.Field);
        }

        var title = Handlebars.Compile(ctx.CurrentPage.Title)(data);
        var body = Handlebars.Compile(ctx.Document.DocumentNode.FirstChild.InnerHtml)(data);
        return template.BuildMainPage(title, body, ctx.CurrentPage.Css);
    }

    private static void SetMetadata(HtmlNode node)
    {
        node.SetAttributeValue(QueryConstants.RecordId, $$$"""{{{{{QueryConstants.RecordId}}}}}""");

        var first = node.FirstChild;

        while (first is { NodeType: HtmlNodeType.Text })
        {
            first = first.NextSibling;
        }

        first.SetAttributeValue(QueryConstants.RecordId, $$$"""{{{{{QueryConstants.RecordId}}}}}""");
        first.SetAttributeValue(SpanConstants.Cursor, $$$"""{{{{{SpanConstants.Cursor}}}}}""");
        first.SetAttributeValue(SpanConstants.HasNextPage, $$$"""{{{{{SpanConstants.HasNextPage}}}}}""");
        first.SetAttributeValue(SpanConstants.HasPreviousPage, $$$"""{{{{{SpanConstants.HasPreviousPage}}}}}""");
    }

    private record PageProcessingContext(Page CurrentPage, HtmlDocument Document)
    {
        public PartialPageContext LoadPartialContext(string elementId)
        {
            var htmlElement = Document.GetElementbyId(elementId);
            return new PartialPageContext(CurrentPage, htmlElement, htmlElement.GetDataNodesIncludeRoot().Ok());
        }

        public FullPageContext ParseDataNodes() =>
            new(CurrentPage, Document, Document.DocumentNode.GetDataNodes().Ok());
    }

    private record FullPageContext(Page CurrentPage, HtmlDocument Document, DataNode[] DataNodes);

    private record PartialPageContext(Page CurrentPage, HtmlNode Element, DataNode[] DataNodes);

    private async Task<PageProcessingContext> LoadPage(string pageName, bool matchPrefix, StrArgs arguments,
        CancellationToken cancellationToken)
    {
        var publicationStatus = PublicationStatusHelper.GetSchemaStatus(arguments);
        var pageSchema = await pageResolver.GetPage(pageName, matchPrefix, publicationStatus, cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(pageSchema.Settings.Page!.Html);
        return new PageProcessingContext(pageSchema.Settings.Page!, document);
    }
}