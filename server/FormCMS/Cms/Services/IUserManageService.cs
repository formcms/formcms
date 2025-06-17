using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;

public interface IUserManageService
{
    Task<PublicUserInfo[]> GetPublicUserInfos(IEnumerable<string> userIds,CancellationToken ct);
    Task<string> GetCreatorId(string tableName, string primaryKey, long recordId, CancellationToken ct);
}