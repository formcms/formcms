using FormCMS.Utils.PageRender;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.StrArgsExt;
using HandlebarsDotNet;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Services;

public sealed class PageService(ILogger<PageService> logger,ISchemaService schemaSvc, IQueryService querySvc, PageTemplate template) : IPageService
{
    public async Task<string> GetDetail(string name, string param, StrArgs strArgs, CancellationToken ct)
    {
        //detail page format <pageName>/{<routerName>}, match with prefix '/{'. 
        var prefix = name + "/{";
        var ctx = (await GetContext(prefix , true, strArgs,ct)).Ok().ToPageContext();
        strArgs = GetLocalPaginationArgs(ctx, strArgs); 
        
        var routerName =ctx.Page.Name.Split("/").Last()[1..^1]; // remove '{' and '}'
        strArgs[routerName] = param;

        var data = string.IsNullOrWhiteSpace(ctx.Page.Query)
            ? new Dictionary<string, object>()
            : await querySvc.SingleWithAction(ctx.Page.Query, strArgs, ct)
              ?? throw new ResultException($"Could not data with {routerName} [{param}]");
        
        return await RenderPage(ctx, data, strArgs, ct);
    }

    public async Task<string> Get(string name, StrArgs strArgs, CancellationToken token)
    {
        if ((await GetContext(name, false, strArgs,token)).Try(out var ctx, out var error))
        {
            return await RenderPage(ctx.ToPageContext(), new Dictionary<string, object>(), strArgs, token);
        }

        var msg = string.Join(",", error!.Select(x => x.Message));
        if (name != "home")
        {
            throw new ResultException(msg);
        }

        logger.LogError("Fail to load page [{page}], err: {err}", name, msg);
        return $"""
                <a href="/admin">Log in to Admin</a><br/>
                <a href="/schema">Go to Schema Builder</a>
                """;
    }

    public async Task<string> GetPart(string partStr, CancellationToken token)
    {
        var part = PagePartHelper.Parse(partStr) ?? throw new ResultException("Invalid Partial Part");
        var cursor = new Span(part.First, part.Last);
        
        var args = QueryHelpers.ParseQuery(part.DataSource.QueryString);
        var ctx = (await GetContext(part.Page, false,args, token)).Ok().ToPartialContext(part.NodeId);

        Record[] items;
        if (!string.IsNullOrWhiteSpace(part.DataSource.Query))
        {
            var pagination = new Pagination(null, part.DataSource.Limit.ToString());
            items = await querySvc.ListWithAction(part.DataSource.Query, cursor, pagination, args, token);
        }
        else
        {
            items = await querySvc.Partial(ctx.Page.Query!, part.DataSource.Field, cursor, part.DataSource.Limit, args, token);
        }

        var flatField = RenderUtil.Flat(part.DataSource.Field);
        var data = new Dictionary<string, object>
        {
            [flatField] = items
        };
        TagPagination(data, items, part);
        
        ctx.Node.SetPaginationTemplate(flatField, part.DataSource.PaginationMode);
        var html = part.DataSource.PaginationMode == PaginationMode.Button
            ? ctx.Node.OuterHtml // for button pagination, replace the div 
            : ctx.Node.InnerHtml; // for infinite screen, append to original div
        var render = Handlebars.Compile(html);
        return render(data);
    }

    private async Task<string> RenderPage(PageContext ctx, Record data, StrArgs args, CancellationToken token)
    {
        await LoadRelatedData(data, args, ctx.Nodes, token);
        TagPagination(ctx, data,args);

        foreach (var repeatNode in ctx.Nodes)
        {
            repeatNode.HtmlNode.SetPaginationTemplate(repeatNode.DataSource.Field, repeatNode.DataSource.PaginationMode);
        }

        var title = Handlebars.Compile(ctx.Page.Title)(data);
        var body = ctx.HtmlDocument.RenderBody(data);
        return template.Build(title, body, ctx.Page.Css);
    }

    private static StrArgs GetLocalPaginationArgs(PageContext ctx,StrArgs strArgs)
    {
        var ret = new StrArgs(strArgs);
        foreach (var node in ctx.Nodes.Where(x => 
                string.IsNullOrWhiteSpace(x.DataSource.Query) && (x.DataSource.Offset > 0 || x.DataSource.Limit > 0)))
        {
            ret[node.DataSource.Field + PaginationConstants.OffsetSuffix] = node.DataSource.Offset.ToString();
            ret[node.DataSource.Field + PaginationConstants.LimitSuffix] = node.DataSource.Limit.ToString();
        }
        return ret;
    }

    private async Task LoadRelatedData(Record data, StrArgs args, DataNode[] nodes, CancellationToken token)
    {
        foreach (var node in nodes.Where(x => !string.IsNullOrWhiteSpace(x.DataSource.Query)))
        {
            var pagination = new Pagination(node.DataSource.Offset.ToString(), node.DataSource.Limit.ToString());
            var result = await querySvc.ListWithAction(node.DataSource.Query, new Span(), pagination, node.MergeArgs(args), token);
            data[node.DataSource.Field] = result;
        }
    }

    private static void TagPagination(PageContext ctx, Record data, StrArgs args)
    {
        foreach (var node in ctx.Nodes.Where(x => x.DataSource.Offset > 0 || x.DataSource.Limit > 0))
        {
            if (data.ByJsonPath<Record[]>(node.DataSource.Field, out var value))
            {
                var nodeWithArg = node with
                {
                    DataSource = node.DataSource with { QueryString = node.MergeArgs(args).ToQueryString() }
                };
                TagPagination(data, value!, nodeWithArg.ToPagePart(ctx.Page.Name));
            }
        }
    }

    private static void TagPagination(Record data, Record[] items, PagePart token)
    {
        if (SpanHelper.HasPrevious(items))
        {
            data[RenderUtil.FirstAttrTag(token.DataSource.Field) ] = PagePartHelper.ToString((token with { First = SpanHelper.FirstCursor(items), Last = ""}));
        }

        if (SpanHelper.HasNext(items))
        {
            data[RenderUtil.LastAttrTag(token.DataSource.Field)] = PagePartHelper.ToString((token with { Last = SpanHelper.LastCursor(items),First = ""}));
        }
    }
    record Context(Page Page, HtmlDocument Doc)
    {
        public PartialContext ToPartialContext(string nodeId) => new (Page, Doc.GetElementbyId(nodeId));
    
        public PageContext ToPageContext() => new (Page, Doc, Doc.GetDataNodes().Ok());
    }

    record PageContext(Page Page, HtmlDocument HtmlDocument, DataNode[] Nodes);
    
    record PartialContext(Page Page, HtmlNode Node);
    
    private async Task<Result<Context>> GetContext(string name, bool matchPrefix,StrArgs args, CancellationToken token)
    {
        var publicationStatus= PublicationStatusHelper.GetSchemaStatus(args);
        var schema = matchPrefix
            ? await schemaSvc.GetByNamePrefixDefault(name, SchemaType.Page,publicationStatus, token)
            : await schemaSvc.GetByNameDefault(name, SchemaType.Page,publicationStatus, token);
        
        if (schema == null) { return Result.Fail($"Cannot find schema [{name}], matching prefix= {matchPrefix}"); }

        var page = schema.Settings.Page!;
        var doc = new HtmlDocument();
        doc.LoadHtml(page.Html);
        return new Context(page, doc);
    }
}

