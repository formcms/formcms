using FormCMS.Auth.Services;

namespace FormCMS.Auth.Handlers;

public static class LoginHandler
{
    public record RegisterReq(string Email, string Password, string UserName);
    public record LoginReq(string usernameOrEmail, string Password);
    
    public static void MapLoginHandlers(this RouteGroupBuilder app)
    {
        
        app.MapPost("/login", async ( ILoginService s, LoginReq req,HttpContext context) =>
        {
            await s.Login(req.usernameOrEmail, req.Password, context);
        });
        
        app.MapPost("/register", async ( ILoginService s, RegisterReq req )
            => await s.Register(req.UserName, req.Email, req.Password));
        
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