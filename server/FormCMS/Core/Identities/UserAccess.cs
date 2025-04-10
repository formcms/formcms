namespace FormCMS.Core.Identities;

public sealed record UserAccess(
    string Id,
    string Email,
    string Name,
    string[] Roles,
    string[] ReadWriteEntities,
    string[] ReadonlyEntities,
    string[] RestrictedReadWriteEntities,
    string[] RestrictedReadonlyEntities,
    string[] AllowedMenus,
    bool CanAccessAdmin = false
);

public static class UserAccessExtensions
{
    public static UserAccess CanAccessAdmin(this UserAccess user)
        => user with
        {
            CanAccessAdmin = user.Roles.Length != 0
                             || user.ReadonlyEntities.Length != 0
                             || user.RestrictedReadonlyEntities.Length != 0
                             || user.ReadWriteEntities.Length != 0
                             || user.RestrictedReadWriteEntities.Length != 0
        };
}