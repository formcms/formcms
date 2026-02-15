using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.HttpContextExt;

namespace FormCMS.Cms.Handlers;

public static class PageHandler
{
    public static RouteHandlerBuilder MapHomePage(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/", async (
            IPageService pageService,
            HttpContext context,
            string? node,
            long ? source,
            string? first,
            string? last,
            CancellationToken ct
        ) =>
        {
            var html = await pageService.Get(PageConstants.Home, context.Args(), 
                nodeId:node,
                sourceId:source,
                span: new Span(first,last),
                ct: ct);
            if (string.IsNullOrWhiteSpace(html)) 
                return Results.NotFound();
            await context.Html(html , ct);
            return Results.Empty;
        });
    }

    public static RouteGroupBuilder MapPages(this RouteGroupBuilder app, params string[] knownUrls)
    {
        var excludedUrls = string.Join("|", knownUrls.Select(x => x.Replace("/", "")));
        var prefix = $"/{{page:regex(^(?!({excludedUrls})).*)}}";
        
        app.MapGet(prefix, async (
            IPageService pageService,
            HttpContext context,
            string page,
            string? node,
            long ? source,
            string? first,
            string? last,
            CancellationToken ct
        ) =>
        {
            var html = await pageService.Get(page, context.Args(),
                nodeId:node,
                sourceId:source,
                span: new Span(first,last),
                ct: ct);
            if (string.IsNullOrWhiteSpace(html)) 
                return Results.NotFound();
            await context.Html(html, ct);
            return Results.Empty;
        });

        
        app.MapGet(prefix + "/{slug}", async (
            IPageService pageService,
            HttpContext context,
            string page,
            string slug,
            string? node,
            long ? source,
            string? first,
            string? last,
            CancellationToken ct
        ) =>
        {
            var html = await pageService.GetDetail(page, slug, context.Args(),
                nodeId:node,
                sourceId:source,
                span: new Span(first,last),
                ct:ct);
            if (string.IsNullOrWhiteSpace(html)) 
                return Results.NotFound();
            await context.Html(html, ct);
            return Results.Empty;
        });
        return app;
    }
}