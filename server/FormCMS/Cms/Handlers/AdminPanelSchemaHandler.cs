using FormCMS.Cms.Services;

namespace FormCMS.Cms.Handlers;

public static class AdminPanelSchemaHandler
{
    public static RouteGroupBuilder MapAdminPanelSchemaHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/menu/{name}", (
            IAdminPanelSchemaService service,
            string name,
            CancellationToken ct
        ) => service.GetMenu(name, ct));


        app.MapGet("/entity/{name}", (
            IAdminPanelSchemaService service,
            string name,
            CancellationToken ct
        ) => service.GetEntity(name, ct));
        
        return app;
    }
}