using FluentResults;
using FormCMS.Utils.HttpClientExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.ApiClient;

public class AuthApiClient(HttpClient client)
{
    private const string Sa ="sadmin@cms.com";
    private const string Admin = "admin@cms.com";
    private const string Pwd = "Admin1!";
    
    
    public Task<Result> EnsureSaLogin() => Login(Sa, Pwd);

    private Task<Result> Login(string email, string password)
    {
        var loginData = new {email, password};
        return client.PostAndSaveCookie("/api/login?useCookies=true", loginData);
    }

    public async Task Logout()
    {
        await client.GetResult("/api/logout").Ok();
        client.DefaultRequestHeaders.Remove("Cookie");
    }

    public async Task Register(string email, string password)
    {
        var loginData = new { email, password };
        await client.PostResult("/api/register", loginData).Ok();
    }

    public async Task RegisterAndLogin(string email, string password)
    {
        var loginData = new { email, password};
        await client.PostResult("/api/register", loginData ).Ok();
        await client.PostAndSaveCookie("/api/login?useCookies=true", loginData).Ok();
    }

    public async Task Sudo(string email, string password, Func<Task> action)
    {
        await Login(email, password);
        await action();
        await Logout();
    }
   
    public  Task SaDo(Func<Task> action) => Sudo(Sa,Pwd,action);
    public  Task AdminDo(Func<Task> action) => Sudo(Admin,Pwd,action);
    
}