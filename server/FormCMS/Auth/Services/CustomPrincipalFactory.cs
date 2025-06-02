using System.Security.Claims;
using FormCMS.Auth.Models;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FormCMS.Auth.Services;

public class CustomPrincipalFactory(
    UserManager<CmsUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<CmsUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(CmsUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            identity.AddClaim(new Claim(nameof(CmsUser.AvatarPath).Camelize(), user.AvatarPath));
        }

        return identity;
    } 
}