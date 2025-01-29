namespace FormCMS.Auth.DTO;

public sealed record UserDto(
    string Id ,
    string Email ,
    string Name ,
    string[] Roles ,
    string[] ReadWriteEntities ,
    string[] RestrictedReadWriteEntities ,
    string[] ReadonlyEntities ,
    string[] RestrictedReadonlyEntities ,
    string[] AllowedMenus 
);


public static class SystemMenus
{
    public const string MenuSchemaBuilder = "menu_schema_builder";
    public const string MenuUsers = "menu_users";
    public const string MenuRoles = "menu_roles";
}