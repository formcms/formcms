using FormCMS.Auth.Models;
using FormCMS.Core.Plugins;
using FormCMS.Core.Identities;

namespace FormCMS.Cms.Services;

public class DummyIdentityService(PluginRegistry registry): IIdentityService
{
    public UserAccess GetUserAccess()
    {
        return new UserAccess
        (
            CanAccessAdmin:true,
            Id:"",
            Name:"admin",
            Email : "sadmin@cms.com",
            AvatarUrl:"",
            Roles : [Roles.Sa],
            AllowedMenus :[..registry.FeatureMenus],
            ReadonlyEntities:[],
            RestrictedReadonlyEntities:[],
            ReadWriteEntities:[],
            RestrictedReadWriteEntities:[]
        );
    }

    public Task<PublicUserInfo[]> GetPublicUserInfos(IEnumerable<string> userIds, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}