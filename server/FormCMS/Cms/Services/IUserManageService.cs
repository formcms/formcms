using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;

public interface IUserManageService
{
    Task<PublicUserInfo[]> GetPublicUserInfos(IEnumerable<string> userIds,CancellationToken ct);
}