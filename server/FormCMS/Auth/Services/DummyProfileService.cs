using FormCMS.Auth.DTO;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.Services;

public class DummyProfileService: IProfileService
{
    public UserDto GetInfo()
    {
        return new UserDto
        (
            Id:"",
            Email : "sadmin@cms.com",
            Roles : [RoleConstants.Sa],
            AllowedMenus : [SystemMenus.MenuSchemaBuilder],
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
}