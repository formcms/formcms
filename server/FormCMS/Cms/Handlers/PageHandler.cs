using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.HttpContextExt;

namespace FormCMS.Cms.Handlers;

public static class PageHandler
{
    public static IApplicationBuilder UseHomePage(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {

            var path = context.Request.Path.Value?.Trim('/');
            if (!string.IsNullOrEmpty(path))
            {
                await next();
                return;
            }

            var pageService = context.RequestServices.GetRequiredService<IPageService>();
            var html = await pageService.Get(
                "home",
                context.Args(),
                ct: context.RequestAborted
            );

            if (!string.IsNullOrWhiteSpace(html))
            {
                await context.Html(html, context.RequestAborted);
                return;
            }

            // 🔥 Important: continue pipeline
            await next();
        });
    }

    public static IApplicationBuilder UsePages(this IApplicationBuilder app, string prefix, string []know)
    {
        prefix = prefix.Trim('/');
        HashSet<string> set = new HashSet<string>(know);
        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value?.Trim('/');

            if (string.IsNullOrEmpty(path) || set.Contains(path))
            {
                await next();
                return;
            }

            // Must start with prefix
            if (!string.IsNullOrWhiteSpace(prefix) && !path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var remaining = path.Substring(prefix.Length).Trim('/');
            if (string.IsNullOrEmpty(remaining))
            {
                await next();
                return;
            }

            var segments = remaining.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var pageService = context.RequestServices.GetRequiredService<IPageService>();

            string? html = null;

            if (segments.Length == 1)
            {
                // /prefix/page
                var page = segments[0];

                html = await pageService.Get(
                    page,
                    context.Args(),
                    nodeId: context.Request.Query["node"],
                    sourceId: long.TryParse(context.Request.Query["source"], out var s) ? s : null,
                    span: new Span(
                        context.Request.Query["first"],
                        context.Request.Query["last"]
                    ),
                    ct: context.RequestAborted
                );
            }
            else if (segments.Length >= 2)
            {
                // /prefix/page/slug
                var page = segments[0];
                var slug = segments[1];

                html = await pageService.GetDetail(
                    page,
                    slug,
                    context.Args(),
                    nodeId: context.Request.Query["node"],
                    sourceId: long.TryParse(context.Request.Query["source"], out var s) ? s : null,
                    span: new Span(
                        context.Request.Query["first"],
                        context.Request.Query["last"]
                    ),
                    ct: context.RequestAborted
                );
            }

            if (!string.IsNullOrWhiteSpace(html))
            {
                await context.Html(html, context.RequestAborted);
                return;
            }

            await next();
        });
    }
}