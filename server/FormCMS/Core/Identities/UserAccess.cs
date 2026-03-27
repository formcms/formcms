using System.Security.Claims;
using FormCMS.Auth.Models;

namespace FormCMS.Core.Identities;

public sealed record UserAccess(
    string Id,
    string Email,
    string Name,
    string AvatarUrl,
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
    {
        if (user.Roles.Contains(Roles.Guest))
        {
            return user;
        }

        return user with
        {
            CanAccessAdmin = user.Roles.Length != 0 || user.ReadonlyEntities.Length != 0
                                                    || user.RestrictedReadonlyEntities.Length != 0
                                                    || user.ReadWriteEntities.Length != 0
                                                    || user.RestrictedReadWriteEntities.Length != 0
        };
    }

    public static Claim[] ToClaims(this UserAccess userResult)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userResult.Id),
            new(ClaimTypes.Name, userResult.Name),
            new(ClaimTypes.Email, userResult.Email)
        };

        foreach (var role in userResult.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var entity in userResult.ReadWriteEntities)
        {
            claims.Add(new Claim(AccessScope.FullAccess, entity));
        }

        foreach (var entity in userResult.RestrictedReadWriteEntities)
        {
            claims.Add(new Claim(AccessScope.RestrictedAccess, entity));
        }
        
        foreach (var entity in userResult.ReadonlyEntities)
        {
            claims.Add(new Claim(AccessScope.FullRead, entity));
        }

        foreach (var entity in userResult.RestrictedReadonlyEntities)
        {
            claims.Add(new Claim(AccessScope.RestrictedRead, entity));
        }

        return claims.ToArray();
    }
}
