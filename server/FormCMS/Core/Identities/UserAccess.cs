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