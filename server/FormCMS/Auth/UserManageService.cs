using FormCMS.Auth.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Auth;

public class UserManageService<TUser>(
    KateQueryExecutor executor,
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

    public async Task<string> GetCreatorId(string tableName, string primaryKey, long recordId, CancellationToken ct)
    {
        var query = new SqlKata.Query(tableName)
            .Where(primaryKey, recordId)
            .Select(Constants.CreatedBy);
        
        var record = await executor.Single(query, CancellationToken.None);
        if (record is not null 
            && record.TryGetValue(Constants.CreatedBy, out var createdBy) && 
            createdBy is  string s)
        {
            return s;
        }
        throw new ResultException("Can't find record's creator");
    }
}