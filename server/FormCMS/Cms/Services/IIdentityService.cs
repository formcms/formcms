using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;


public interface IIdentityService
{
    UserAccess? GetUserAccess();
    Task<PublicUserInfo[]> GetPublicUserInfos(IEnumerable<string> userIds,CancellationToken ct);
}