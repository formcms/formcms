using System.Security.Claims;
using FormCMS.Cms.Services;
using FormCMS.Core.Identities;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;

namespace FormCMS.Auth.Services;

public sealed record ProfileDto(string OldPassword, string Password);


public class ProfileService<TUser>(
    UserManager<TUser> userManager,
    IHttpContextAccessor contextAccessor,
    RestrictedFeatures restrictedFeatures,
    SignInManager<TUser> signInManager
    ):IProfileService
where TUser :IdentityUser, new()
{
    public UserAccess? GetInfo()
    {
        
        var claimsPrincipal = contextAccessor.HttpContext?.User;
        if (claimsPrincipal?.Identity?.IsAuthenticated != true) return null;

        string[] roles = [..claimsPrincipal.FindAll(ClaimTypes.Role).Select(x => x.Value)];
        
        return new UserAccess
        (
            Id: claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
            Name: claimsPrincipal.Identity.Name??"",
            Email: claimsPrincipal.FindFirstValue(ClaimTypes.Email) ?? "",
            Roles: roles,
            ReadWriteEntities: [..claimsPrincipal.FindAll(AccessScope.FullAccess).Select(x => x.Value)],
            RestrictedReadWriteEntities: [..claimsPrincipal.FindAll(AccessScope.RestrictedAccess).Select(x => x.Value)],
            ReadonlyEntities: [..claimsPrincipal.FindAll(AccessScope.FullRead).Select(x => x.Value)],
            RestrictedReadonlyEntities: [..claimsPrincipal.FindAll(AccessScope.RestrictedRead).Select(x => x.Value)],
            AllowedMenus: roles.Contains(Roles.Sa) || roles.Contains(Roles.Admin) ? restrictedFeatures.Menus.ToArray() : []
        );
    }
    
    public async Task ChangePassword(ProfileDto dto)
    {
        var user = await MustGetCurrentUser();
        var result =await userManager.ChangePasswordAsync(user, dto.OldPassword, dto.Password);
        if (!result.Succeeded) throw new ResultException(IdentityErrMsg(result));
    }
    
    public AccessLevel MustGetReadWriteLevel(string entityName)
    {
        var access = GetInfo();
        if (access.HasRole(Roles.Sa) || access.CanFullReadWrite(entityName))
        {
            return AccessLevel.Full;
        }

        if (access.CanRestrictedReadWrite(entityName))
        {
            return AccessLevel.Restricted;
        }

        throw new ResultException("You don't have permission to read [" + entityName + "]");
    }

    public AccessLevel MustGetReadLevel(string entityName)
    {
        var access = GetInfo();
        if (access.HasRole(Roles.Sa) || access.CanFullReadOnly(entityName) || access.CanFullReadWrite(entityName))
        {
            return AccessLevel.Full;
        }

        if ( access.CanRestrictedReadOnly(entityName) || access.CanRestrictedReadWrite(entityName))
        {
            return AccessLevel.Restricted;
        }
        throw new ResultException("You don't have permission to read [" + entityName + "]");
    }
    
    public async Task EnsureCurrentUserHaveEntityAccess(string entityName)
    {
        var user = await userManager.GetUserAsync(contextAccessor.HttpContext!.User) ??
                   throw new Exception("User not found.");

        var claims = await userManager.GetClaimsAsync(user);

        var hasAccess = claims.Any(claim => 
            claim.Value == entityName && 
            claim.Type is AccessScope.RestrictedAccess or AccessScope.FullAccess
        );

        if (!hasAccess)
        {
            await userManager.AddClaimAsync(user, new Claim(AccessScope.RestrictedAccess, entityName));
            await signInManager.RefreshSignInAsync(user);
        }
    }

    private async Task<TUser> MustGetCurrentUser()
    {
        var claims = contextAccessor.HttpContext?.User;
        if (claims?.Identity?.IsAuthenticated != true) throw new ResultException("Not logged in");
        var user =await userManager.GetUserAsync(claims);
        return user?? throw new ResultException("Not logged in");
    }
    private static string IdentityErrMsg(IdentityResult result
    ) =>  string.Join("\r\n", result.Errors.Select(e => e.Description));

}