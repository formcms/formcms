using Microsoft.AspNetCore.Authentication.OAuth;

namespace FormCMS.Auth.Services;

public interface ILoginService
{
    Task Login(string userName, string password, HttpContext httpContext);
    Task Register(string username, string email, string password);
    Task Logout();
    Task ExternalLogin(string provider, string returnUrl);
    Task HandleGithubCallback(OAuthCreatingTicketContext context);
}