using Azure.Core;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.HttpContextExt;
using MongoDB.Driver.Core.Operations.ElementNameValidators;

namespace FormCMS.Cms.Handlers;

public static class PageHandler
{
    public static RouteHandlerBuilder MapHomePage(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/", async (
            IPageService pageService,
            HttpContext context,
            CancellationToken ct
        ) => await context.Html(await pageService.Get(PageConstants.Home, context.Args(), ct), ct));
    }

    public static RouteGroupBuilder MapPages(this RouteGroupBuilder app, params string[] knownUrls)
    {
        var excludedUrls = string.Join("|", knownUrls.Select(x => x.Replace("/", "")));
        var prefix = $"/{{page:regex(^(?!({excludedUrls})).*)}}";
        
        app.MapGet("/page_part", async (
            IPageService pageService,
            HttpContext context,
            long ?source,
            string token,
            bool replace,
            CancellationToken ct
        ) => await context.Html(await pageService.GetPart(source,token, replace, ct), ct));

        app.MapGet(prefix, async (
            IPageService pageService,
            HttpContext context,
            string page,
            CancellationToken ct
        ) => await context.Html(await pageService.Get(page, context.Args(), ct), ct));

        app.MapGet(prefix + "/{slug}", async (
            IPageService pageService,
            HttpContext context,
            string page,
            string slug,
            CancellationToken ct
        ) => await context.Html(await pageService.GetDetail(page, slug, context.Args(), ct), ct));
        return app;
    }
}