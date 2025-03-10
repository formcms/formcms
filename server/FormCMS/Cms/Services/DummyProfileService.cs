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
            Id:"",
            Name:"admin",
            Email : "sadmin@cms.com",
            Roles : [..restrictedFeatures.Menus],
            AllowedMenus : [],
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

    public async Task EnsureCurrentUserHaveEntityAccess(string entityName)
    {
    }
}