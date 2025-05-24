using FormCMS.Auth.Services;
using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;

public interface IProfileService
{
    UserAccess? GetUserAccess();
    PublicProfile? GetUserProfile();
    Task ChangePassword(string password, string newPassword);
    AccessLevel MustGetReadWriteLevel(string entityName);
    AccessLevel MustGetReadLevel(string entityName);
    void MustHasAnyRole(IEnumerable<string> role);
    Task EnsureCurrentUserHaveEntityAccess(string entityName);
    bool HasRole(string role);
    Task<PublicProfile> GetProfiles(IEnumerable<string> userIds);
    Task<string> UploadAvatar(IFormFile file, CancellationToken ct);
}