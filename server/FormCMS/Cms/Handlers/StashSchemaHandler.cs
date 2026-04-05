using FormCMS.Cms.Services;

namespace FormCMS.Cms.Handlers;

public static class StashSchemaHandler
{
    public static RouteGroupBuilder MapStashSchemaHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/public-queries", (
            IStashSchemaService service,
            CancellationToken ct
        ) => service.GetQueries(ct));
        return app;
    }
}