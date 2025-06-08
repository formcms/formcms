using System.Security.Claims;
using FormCMS.Auth.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Plugins;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.FileStore;
using Humanizer;

namespace FormCMS.Auth.Services;

public class IdentityService(
    IFileStore store,
    IHttpContextAccessor contextAccessor,
    PluginRegistry registry
) : IIdentityService

{
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
            AvatarUrl: store.GetUrl(contextUser.FindFirstValue(nameof(CmsUser.AvatarPath).Camelize())??""),
            ReadWriteEntities: [..contextUser.FindAll(AccessScope.FullAccess).Select(x => x.Value)],
            RestrictedReadWriteEntities: [..contextUser.FindAll(AccessScope.RestrictedAccess).Select(x => x.Value)],
            ReadonlyEntities: [..contextUser.FindAll(AccessScope.FullRead).Select(x => x.Value)],
            RestrictedReadonlyEntities: [..contextUser.FindAll(AccessScope.RestrictedRead).Select(x => x.Value)],
            AllowedMenus: roles.Contains(Roles.Sa) || roles.Contains(Roles.Admin)
                ? [..registry.FeatureMenus]
                : []
        );
        
        return user.CanAccessAdmin();
    }

  
}