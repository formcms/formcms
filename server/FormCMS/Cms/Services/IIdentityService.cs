using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;


public interface IIdentityService
{
    UserAccess? GetUserAccess();
}