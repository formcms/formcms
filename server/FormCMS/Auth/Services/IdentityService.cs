using System.Security.Claims;
using FormCMS.Auth.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.FileStore;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Auth.Services;

public class IdentityService<TUser>(
    IFileStore store,
    IHttpContextAccessor contextAccessor,
    UserManager<TUser> userManager,
    RestrictedFeatures restrictedFeatures
) : IIdentityService
    where TUser : CmsUser, new()

{
    private const string DefaultUrl = "/_content/FormCMS/static-assets/imgs/avatar.jpg";
    public UserAccess? GetUserAccess()
    {
        var contextUser = contextAccessor.HttpContext?.User;
        if (contextUser?.Identity?.IsAuthenticated != true) return null;

        string[] roles = [..contextUser.FindAll(ClaimTypes.Role).Select(x => x.Value)];

        var user = new UserAccess
        (
            Id: contextUser.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
            Name: contextUser.Identity.Name ?? "",
            Email: contextUser.FindFirstValue(ClaimTypes.Email) ??"",
            Roles: roles,
            AvatarUrl: store.GetUrl(contextUser.FindFirstValue(nameof(CmsUser.AvatarPath).Camelize()) ?? DefaultUrl),
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

    public async Task<PublicUserInfo[]> GetPublicUserInfos(IEnumerable<string> userIds,CancellationToken ct)
    {
        var users =   await userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);
        return users.Select(x =>
            new PublicUserInfo(x.Id, x.UserName ?? "", x.AvatarPath is not null ? store.GetUrl(x.AvatarPath) : DefaultUrl))
            .ToArray();
    }
}