using FormCMS.Cms.Services;

namespace FormCMS.Cms.Handlers;

public static class PageDataHandler
{
    public static RouteGroupBuilder MapPageData(this RouteGroupBuilder app)
    {
       app.MapGet("/", (IPageService svc, string id, CancellationToken ct) => svc.GetAiPageData(id,ct));
       return app;
    }
}