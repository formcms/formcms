namespace FormCMS.Auth.Services;

public interface ILoginService
{
    Task Logout();
    Task ExternalLogin(string provider, string returnUrl);
}