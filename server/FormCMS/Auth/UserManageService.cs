using FormCMS.Auth.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.FileStore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Auth;

public class UserManageService<TUser>(
    UserManager<TUser> userManager,
    IFileStore store
    ):IUserManageService
    where TUser : CmsUser, new()
{
    private const string DefaultUrl = "/_content/FormCMS/static-assets/imgs/avatar.jpg";
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