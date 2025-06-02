using System.Security.Claims;
using FormCMS.Auth.Models;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;

namespace FormCMS.Auth.Services;

public class LoginService<TUser>(    
    IHttpContextAccessor contextAccessor,
    SignInManager<TUser> signInManager,
    UserManager<TUser> userManager
    )
    : ILoginService
    where TUser : CmsUser, new()
{
    public async Task Login(string usernameOrEmail, string password, HttpContext context)
    {
        var user = usernameOrEmail.IndexOf('@') > -1
            ? await userManager.FindByEmailAsync(usernameOrEmail)
            : await userManager.FindByNameAsync(usernameOrEmail);
        if (user is null) throw new ResultException("Invalid username or email");
        
        // Attempt to sign in
        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            throw new ResultException("Invalid username or password");
        }

        // Manually sign in to create authentication cookie
        await signInManager.SignInAsync(user, isPersistent: true); 
    }

    public async Task HandleGithubCallback(OAuthCreatingTicketContext context)
    {
        var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value
                 ?? context.User.GetProperty("email").GetString();

        if (string.IsNullOrEmpty(email))
        {
            throw new Exception("Email not found from GitHub.");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new TUser { UserName = context.Identity?.Name, Email = email };
            await userManager.CreateAsync(user);
        }
        await signInManager.SignInAsync(user, isPersistent: false);
    }
    
    public async Task Register(string username, string email, string password)
    {
        var user = new TUser
        {
            UserName = username,
            Email = email
        };
        
        var res = await userManager.CreateAsync(user, password);
        if (!res.Succeeded)
        {
            throw new ResultException(string.Join(",", res.Errors.Select(x => x.Description)));
        }
    }

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