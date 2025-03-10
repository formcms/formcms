namespace FormCMS.Core.Identities;

public sealed record UserAccess(
    string Id ,
    string Email ,
    string Name ,
    string[] Roles ,
    string[] ReadWriteEntities ,
    string[] ReadonlyEntities ,
    
    string[] RestrictedReadWriteEntities ,
    string[] RestrictedReadonlyEntities ,
    string[] AllowedMenus 
);

public static class UserAccessHelper
{
    public static bool HasRole(this UserAccess? userAccess, string role)
        => userAccess?.Roles.Contains(role) ?? false;

    public static bool CanFullReadOnly(this UserAccess? userAccess, string entityName)
        => userAccess?.ReadonlyEntities.Contains(entityName) ?? false;
    
    public static bool CanRestrictedReadOnly(this UserAccess? userAccess, string entityName)
        => userAccess?.RestrictedReadonlyEntities.Contains(entityName) ?? false;
    
    public static bool CanFullReadWrite(this UserAccess? userAccess, string entityName)
        => userAccess?.ReadWriteEntities.Contains(entityName) ?? false;
    
    public static bool CanRestrictedReadWrite(this UserAccess? userAccess, string entityName)
        => userAccess?.RestrictedReadWriteEntities.Contains(entityName) ?? false;
    
}