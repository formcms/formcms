using FormCMS.Auth;
using FormCMS.Auth.Services;
using FormCMS.Core.Identities;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Services;

public class DummyProfileService(RestrictedFeatures restrictedFeatures): IProfileService
{
    public UserAccess GetInfo()
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
    
    public Task ChangePassword(ProfileDto dto)
    {
        throw new ResultException("Not implemented yet");
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
}