using FormCMS.Auth.Services;
using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;

public interface IProfileService
{
    UserAccess? GetInfo();
    Task ChangePassword(ProfileDto dto);
    AccessLevel MustGetReadWriteLevel(string entityName);
    AccessLevel MustGetReadLevel(string entityName);
    void MustHasAnyRole(IEnumerable<string> role);
    Task EnsureCurrentUserHaveEntityAccess(string entityName);
    bool HasRole(string role);
}