using FormCMS.Auth.Services;
using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;

public interface IProfileService
{
    UserAccess? GetInfo();
    Task ChangePassword(ProfileDto dto);
    AccessLevel MustGetReadWriteLevel(string entityName);
    AccessLevel MustGetReadLevel(string entityName);
    Task EnsureCurrentUserHaveEntityAccess(string entityName);
}