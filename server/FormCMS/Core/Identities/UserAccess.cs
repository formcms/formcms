namespace FormCMS.Core.Identities;

public sealed record UserAccess(
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