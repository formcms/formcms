using Azure.Core;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.HttpContextExt;

namespace FormCMS.Cms.Handlers;

public static class QueryHandlers
{
    public static RouteGroupBuilder MapQueryHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/{name}",  (
            IQueryService svc,
            HttpContext ctx,
            string name,
            string? first,
            string? last,
            string? offset,
            string? limit,
            CancellationToken ct
        ) =>  svc.ListWithAction(name, new Span(first, last), new Pagination(offset, limit), ctx.Args(), ct));

        app.MapGet("/{name}/single",  (
            IQueryService queryService,
            HttpContext httpContext,
            string name,
            CancellationToken token
        ) =>  queryService.SingleWithAction(name, httpContext.Args(), token));

        app.MapGet("/{name}/part/{attr}",  (
            IQueryService svc,
            HttpContext ctx,
            string name,
            string attr,
            long sourceId,
            string? first,
            string? last,
            int limit,
            CancellationToken token
        ) =>  svc.Partial(name, attr,sourceId, new Span(first, last), limit, ctx.Args(), token));
        return app;
    }
}