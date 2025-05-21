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
    SignInManager<TUser> signInManager,
    RestrictedFeatures restrictedFeatures
) : IProfileService
    where TUser : IdentityUser, new()
{
    public UserAccess? GetInfo()
    {

        var contextUser = contextAccessor.HttpContext?.User;
        if (contextUser?.Identity?.IsAuthenticated != true) return null;

        var email = contextUser.FindFirstValue(ClaimTypes.Email);
        if (email is null) return null;
        
        string[] roles = [..contextUser.FindAll(ClaimTypes.Role).Select(x => x.Value)];

        var user = new UserAccess
        (
            Id: contextUser.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
            Name: contextUser.Identity.Name ?? "",
            Email: email,
            Roles: roles,
            ReadWriteEntities: [..contextUser.FindAll(AccessScope.FullAccess).Select(x => x.Value)],
            RestrictedReadWriteEntities: [..contextUser.FindAll(AccessScope.RestrictedAccess).Select(x => x.Value)],
            ReadonlyEntities: [..contextUser.FindAll(AccessScope.FullRead).Select(x => x.Value)],
            RestrictedReadonlyEntities: [..contextUser.FindAll(AccessScope.RestrictedRead).Select(x => x.Value)],
            AllowedMenus: roles.Contains(Roles.Sa) || roles.Contains(Roles.Admin)
                ? restrictedFeatures.Menus.ToArray()
                : []
        );
        
        return user.CanAccessAdmin();
    }

    public async Task ChangePassword(ProfileDto dto)
    {
        var user = await MustGetCurrentUser();
        var result = await userManager.ChangePasswordAsync(user, dto.OldPassword, dto.Password);
        if (!result.Succeeded) throw new ResultException(IdentityErrMsg(result));
    }

    public AccessLevel MustGetReadWriteLevel(string entityName)
    {
        if (HasRole(Roles.Sa) || CanFullReadWrite(entityName))
        {
            return AccessLevel.Full;
        }

        if (CanRestrictedReadWrite(entityName))
        {
            return AccessLevel.Restricted;
        }

        throw new ResultException("You don't have permission to write [" + entityName + "]");
    }

    public AccessLevel MustGetReadLevel(string entityName)
    {
        if (HasRole(Roles.Sa) || CanFullReadOnly(entityName) || CanFullReadWrite(entityName))
        {
            return AccessLevel.Full;
        }

        if (CanRestrictedReadOnly(entityName) || CanRestrictedReadWrite(entityName))
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
    public  void MustHasAnyRole(IEnumerable<string> role)
    {
        if (role.Any(HasRole))
        {
            return;
        }

        throw new ResultException("You don't have permission to do this operation");
    }
    
    private async Task<TUser> MustGetCurrentUser()
    {
        var claims = contextAccessor.HttpContext?.User;
        if (claims?.Identity?.IsAuthenticated != true) throw new ResultException("Not logged in");
        var user = await userManager.GetUserAsync(claims);
        return user ?? throw new ResultException("Not logged in");
    }

    private static string IdentityErrMsg(IdentityResult result
    ) => string.Join("\r\n", result.Errors.Select(e => e.Description));

    private bool CanFullReadOnly(string entityName) => HasClaims(AccessScope.FullRead, entityName);

    private  bool CanRestrictedReadOnly(string entityName) => HasClaims(AccessScope.RestrictedRead, entityName);

    private  bool CanFullReadWrite( string entityName) => HasClaims(AccessScope.FullAccess, entityName);

    private  bool CanRestrictedReadWrite( string entityName) => HasClaims(AccessScope.RestrictedAccess, entityName);
    
    private  bool HasClaims(string claimType, string value)
    {
        var userClaims = contextAccessor.HttpContext?.User;
        if (userClaims?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return userClaims.Claims.FirstOrDefault(x => x.Value == value && x.Type == claimType) != null;
    }
    
    public  bool HasRole(string role)
    {
        return contextAccessor.HttpContext?.User.IsInRole(role) ?? false;
    }
}