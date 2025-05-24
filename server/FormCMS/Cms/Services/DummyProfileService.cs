using FormCMS.Auth.Models;
using FormCMS.Core.Identities;
using AccessLevel = FormCMS.Auth.Services.AccessLevel;

namespace FormCMS.Cms.Services;

public class DummyProfileService(RestrictedFeatures restrictedFeatures): IProfileService
{
    public UserAccess GetUserAccess()
    {
        return new UserAccess
        (
            CanAccessAdmin:true,
            Id:"",
            Name:"admin",
            Email : "sadmin@cms.com",
            Roles : [Roles.Sa],
            AllowedMenus :[..restrictedFeatures.Menus],
            ReadonlyEntities:[],
            RestrictedReadonlyEntities:[],
            ReadWriteEntities:[],
            RestrictedReadWriteEntities:[]
        );
    }

    public PublicProfile? GetUserProfile()
    {
        throw new NotImplementedException();
    }

    public Task ChangePassword(string password, string newPassword)
    {
        throw new NotImplementedException();
    }

    public AccessLevel MustGetReadWriteLevel(string entityName)
    {
        return AccessLevel.Full;
    }

    public AccessLevel MustGetReadLevel(string entityName)
    {
        return AccessLevel.Full;
    }

    public void MustHasAnyRole(IEnumerable<string> role)
    {
    }

    public Task EnsureCurrentUserHaveEntityAccess(string entityName)
    {
        return Task.CompletedTask;
    }

    public bool HasRole(string role)
    {
        return true;
    }

    public Task<PublicProfile> GetProfiles(IEnumerable<string> userIds)
    {
        throw new NotImplementedException();
    }

    public Task<string> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}