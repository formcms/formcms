using System.Security.Claims;
using FormCMS.Auth.Models;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.ImageUtil;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using NUlid;

namespace FormCMS.Auth.Services;

public class ProfileService<TUser>(
    IHttpContextAccessor contextAccessor,
    IFileStore store,
    IResizer resizer,
    UserManager<TUser> userManager,
    SignInManager<TUser> signInManager
    ):IProfileService
    where TUser : CmsUser, new()

{
    public async Task ChangePassword(string password, string newPassword)
    {
        var user = await MustGetCurrentUser();
        var result = await userManager.ChangePasswordAsync(user, password, newPassword);
        if (!result.Succeeded) throw new ResultException(IdentityErrMsg(result));
    }
    
    public async Task UploadAvatar(IFormFile file, CancellationToken ct)
    {
        //delete old avatar
        var user = await MustGetCurrentUser();
        if (user.AvatarPath != null)
        {
            try
            {
                await store.Del(user.AvatarPath, ct);
            }
            catch { //ignore
            }
        }

        if (file.Length ==0)  throw new ResultException($"File [{file.FileName}] is empty");
        file = resizer.CompressImage(file);
        var path = Path.Join("avatar", Ulid.NewUlid().ToString()) + Path.GetExtension(file.FileName);
        await store.Upload([(path,file)],ct);

        user.AvatarPath = path;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
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

    public Models.AccessLevel MustGetReadLevel(string entityName)
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
    public  bool HasRole(string role)
    {
        return contextAccessor.HttpContext?.User.IsInRole(role) ?? false;
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
    
}