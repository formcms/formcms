using FormCMS.Cms.Services;

namespace FormCMS.Cms.Handlers;

public static class IdentityHandler
{
    public static void MapIdentityHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/me", (
            IIdentityService svc
        ) =>
        {
            var info = svc.GetUserAccess();
            return info is not null ? Results.Ok(info) : Results.Unauthorized();
        });
    }
}