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

    private Task<Result> Login(string usernameOrEmail, string password)
    {
        var loginData = new {usernameOrEmail, password};
        return client.PostAndSaveCookie("/api/login", loginData);
    }

    public async Task Logout()
    {
        await client.GetResult("/api/logout").Ok();
        client.DefaultRequestHeaders.Remove("Cookie");
    }

    public async Task Register(string username,string email, string password)
    {
        var loginData = new {username, email, password };
        await client.PostResult("/api/register", loginData).Ok();
    }

    public async Task RegisterAndLogin(string username, string email, string password)
    {
        await client.PostResult("/api/register", new { username, email, password} ).Ok();
        await client.PostAndSaveCookie("/api/login", new {usernameOrEmail= email, password} ).Ok();
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