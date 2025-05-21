using FormCMS.Auth.Services;

namespace FormCMS.Auth.Handlers;

public static class LoginHandler
{
    public static void MapLoginHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/ext_login/{provider}", async (
            ILoginService svc,
            string provider,
            string? returnUrl
        ) =>
        {
            await svc.ExternalLogin(provider, returnUrl??"/");
        });
        
        app.MapGet("/logout", async (
            ILoginService svc
        ) =>
        {
            await svc.Logout();
        });
    }
}