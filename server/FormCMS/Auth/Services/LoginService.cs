using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace FormCMS.Auth.Services;

public class LoginService<TUser>(    
    IHttpContextAccessor contextAccessor,
    SignInManager<TUser> signInManager)
    : ILoginService
    where TUser : IdentityUser, new()
{
    public Task Logout()
    {
        return signInManager.SignOutAsync();
    }

    public Task ExternalLogin(string provider, string returnUrl)
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl};
        return contextAccessor.HttpContext!.ChallengeAsync(provider, props);
    }
    
}