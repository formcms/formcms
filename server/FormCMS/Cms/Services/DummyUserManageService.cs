using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;

public class DummyUserManageService:IUserManageService
{
    public Task<PublicUserInfo[]> GetPublicUserInfos(IEnumerable<string> userIds, CancellationToken ct)
    {
        return Task.FromResult(Array.Empty<PublicUserInfo>());
    }

    public Task<string> GetCreatorId(string tableName, string primaryKey, string recordId, CancellationToken ct)
    {
        return Task.FromResult(primaryKey);
    }
}