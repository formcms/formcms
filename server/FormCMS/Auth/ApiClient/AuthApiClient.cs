using FluentResults;
using FormCMS.Utils.HttpClientExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.ApiClient;

public class AuthApiClient(HttpClient client)
{
    public Task<Result> EnsureSaLogin()
        => Login("sadmin@cms.com", "Admin1!");
    
    public  Task<Result> Login(string email, string password)
    {
        var loginData = new { email, password};
        return client.PostAndSaveCookie("/api/login?useCookies=true", loginData);
    }

    public async Task<Result> Logout()
    {
        await client.GetResult("/api/logout").Ok();
        client.DefaultRequestHeaders.Remove("Cookie");
        return Result.Ok();
    }

    public async Task<Result> Register(string email, string password)
    {
        var loginData = new { email, password};
        await client.PostResult("/api/register", loginData ).Ok();
        return Result.Ok();
    }
}