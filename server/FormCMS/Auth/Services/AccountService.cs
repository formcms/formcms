using System.Security.Claims;
using FluentResults;
using FormCMS.Auth.Models;
using FormCMS.Core.Assets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Auth.Services;

public class AccountService<TUser, TRole,TCtx>(
    UserManager<TUser> userManager,
    RoleManager<TRole> roleManager,
    IProfileService profileService,
    TCtx context,
    KateQueryExecutor queryExecutor
) : IAccountService
    where TUser : IdentityUser, new()
    where TRole : IdentityRole, new()
    where TCtx : IdentityDbContext<TUser>

{
    public async Task<string[]> GetEntities(CancellationToken ct)
    {
        var query = SchemaHelper.ByNameAndType(SchemaType.Entity, null, null);
        var records= await queryExecutor.Many(query,ct);
        var entityName = records.Select(x => (string)x[nameof(Entity.Name).Camelize()]).ToArray();
        return [..entityName, Assets.XEntity.Name];
    }
    
    public async Task<string[]> GetRoles(CancellationToken ct)
    {
        profileService.MustHasAnyRole([Roles.Admin, Roles.Sa]);
        var roles = await context.Roles.Select(x => x.Name??"").ToArrayAsync(ct);
        return roles;
    }

    public async Task<UserAccess> GetSingleUser(string id, CancellationToken ct)
    {
        profileService.MustHasAnyRole([Roles.Admin, Roles.Sa]);
        var query = from user in context.Users
            where user.Id == id 
            join userRole in context.UserRoles
                on user.Id equals userRole.UserId into userRolesGroup
            from userRole in userRolesGroup.DefaultIfEmpty() // Left join for roles
            join role in context.Roles
                on userRole.RoleId equals role.Id into rolesGroup
            from role in rolesGroup.DefaultIfEmpty() // Left join for roles
            join userClaim in context.UserClaims
                on user.Id equals userClaim.UserId into userClaimsGroup
            from userClaim in userClaimsGroup.DefaultIfEmpty() // Left join for claims
            group new { role, userClaim } by user
            into userGroup
            select new { userGroup.Key, Values = userGroup.ToArray() };
        
        // use client calculation to support Sqlite
        var item = await query.FirstOrDefaultAsync(ct)
            ?? throw new ResultException($"Cannot find user by id [{id}]");
        var userAcc = new UserAccess
        (
            Email: item.Key.Email!,
            Id: item.Key.Id,
            Name:item.Key.UserName??"",
            AvatarUrl:"",
            Roles: [..item.Values.Where(x => x.role is not null).Select(x => x.role.Name!).Distinct()],
            ReadWriteEntities:
            [
                ..item.Values
                    .Where(x => x.userClaim?.ClaimType == AccessScope.FullAccess)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            RestrictedReadWriteEntities:
            [
                ..item.Values
                    .Where(x => x.userClaim?.ClaimType == AccessScope.RestrictedAccess)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            ReadonlyEntities:
            [
                ..item.Values
                    .Where(x => x.userClaim?.ClaimType == AccessScope.FullRead)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            RestrictedReadonlyEntities:
            [
                ..item.Values
                    .Where(x => x.userClaim?.ClaimType == AccessScope.RestrictedRead)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            AllowedMenus: []
        );
        return userAcc.CanAccessAdmin();
    }

    public async Task<UserAccess[]> GetUsers(CancellationToken ct)
    {
        profileService.MustHasAnyRole([Roles.Admin, Roles.Sa]);
        var query = from user in context.Users
            join userRole in context.UserRoles
                on user.Id equals userRole.UserId into userRolesGroup
            from userRole in userRolesGroup.DefaultIfEmpty() // Left join for roles
            join role in context.Roles
                on userRole.RoleId equals role.Id into rolesGroup
            from role in rolesGroup.DefaultIfEmpty() // Left join for roles
            group new { role } by user
            into userGroup
            select new {userGroup.Key, Roles =userGroup.ToArray()};
        var items = await query.ToArrayAsync(ct);
        // use client calculation to support Sqlite
        return [..items.Select(x => new UserAccess
        (
            Email : x.Key.Email!,
            Id : x.Key.Id,
            Name : x.Key.UserName??"",
            AvatarUrl:"",
            Roles : [..x.Roles.Where(val=>val?.role is not null).Select(val => val.role.Name!).Distinct()],
            AllowedMenus:[],
            ReadonlyEntities:[],
            ReadWriteEntities:[],
            RestrictedReadonlyEntities:[],
            RestrictedReadWriteEntities:[]
        ).CanAccessAdmin() )];
    }

    public async Task<Result> EnsureUser(string email, string password, string[] roles)
    {
        var result = await EnsureRoles(roles);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return Result.Ok();
        }

        user = new TUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
        };

        var res = await userManager.CreateAsync(user, password);
        if (!res.Succeeded)
        {
            return Fail(res);
        }

        res = await userManager.AddToRolesAsync(user, roles);
        return !res.Succeeded ? Fail(res): Result.Ok();
    }

    public async Task DeleteUser(string id)
    {
        profileService.MustHasAnyRole([Roles.Sa]);
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ResultException($"Fail to Delete User, Cannot find user by id [{id}]");
        Assure(await userManager.DeleteAsync(user));
    }
    
    public async Task SaveUser(UserAccess access)
    {
        profileService.MustHasAnyRole([Roles.Sa]);
        var user = await MustFindUser(access.Id);
        var claims = await userManager.GetClaimsAsync(user);
        await AssignRole(user, access.Roles).Ok();
        await AssignClaim(user, claims, AccessScope.FullAccess, access.ReadWriteEntities).Ok();
        await AssignClaim(user, claims, AccessScope.RestrictedAccess, access.RestrictedReadWriteEntities).Ok();
        await AssignClaim(user, claims, AccessScope.FullRead, access.ReadonlyEntities).Ok();
        await AssignClaim(user, claims, AccessScope.RestrictedRead, access.RestrictedReadonlyEntities).Ok();
    }
    
    public async Task<RoleAccess> GetSingleRole(string name)
    {
        var role = await roleManager.FindByNameAsync(name) ?? throw new ResultException($"Cannot find role by name [{name}]");
        var claims = await roleManager.GetClaimsAsync(role);
        return new RoleAccess
        (
            Name: name,
            ReadWriteEntities: [..claims.Where(x => x.Type == AccessScope.FullAccess).Select(x => x.Value)],
            RestrictedReadWriteEntities:
            [..claims.Where(x => x.Type == AccessScope.RestrictedAccess).Select(x => x.Value)],
            ReadonlyEntities: [..claims.Where(x => x.Type == AccessScope.FullRead).Select(x => x.Value)],
            RestrictedReadonlyEntities: [..claims.Where(x => x.Type == AccessScope.RestrictedRead).Select(x => x.Value)]
        );
    }
    public async Task DeleteRole(string name)
    {
        profileService.MustHasAnyRole([Roles.Sa]);
        if (name is Roles.Admin or Roles.Sa) throw new ResultException($"Cannot delete System Build-in Role [{name}]");
        var role = await roleManager.FindByNameAsync(name)?? throw new ResultException($"Cannot find role by name [{name}]");
        Assure(await roleManager.DeleteAsync(role));
    }

    public async Task SaveRole(RoleAccess roleAccess)
    {
        profileService.MustHasAnyRole([Roles.Sa]);
        if (string.IsNullOrWhiteSpace(roleAccess.Name))
        {
            throw new ResultException("Role name can not be null");
        }
        await EnsureRoles([roleAccess.Name]).Ok();
        var role = await roleManager.FindByNameAsync(roleAccess.Name);
        var claims =await roleManager.GetClaimsAsync(role!);
        await AddClaimsToRole(role!, claims, AccessScope.FullAccess, roleAccess.ReadWriteEntities??[]).Ok();
        await AddClaimsToRole(role!, claims, AccessScope.RestrictedAccess, roleAccess.RestrictedReadWriteEntities??[]).Ok();
        await AddClaimsToRole(role!, claims, AccessScope.FullRead, roleAccess.ReadonlyEntities??[]).Ok();
        await AddClaimsToRole(role!, claims, AccessScope.RestrictedRead, roleAccess.RestrictedReadonlyEntities??[]).Ok();
    }


    private async Task<TUser> MustFindUser(string id)
    {
        return await userManager.Users.FirstOrDefaultAsync(x => x.Id == id) ??
               throw new ResultException($"user not found by id {id}");
    }

    private async Task<Result> AssignClaim(TUser user, IList<Claim> claims, string type, IEnumerable<string> list)
    {
        string[] values = [..list];
        var currentValues = claims.Where(x => x.Type == type).Select(x => x.Value).ToArray();
        // Calculate roles to be removed and added
        var toRemove = currentValues.Except(values).ToArray();
        var toAdd = values.Except(currentValues).ToArray();

        // Remove only the roles that are in currentRoles but not in the new roles
        if (toRemove.Any())
        {
            var result = await userManager.RemoveClaimsAsync(user, toRemove.Select(x=>new Claim(type, x)));
            if (!result.Succeeded)
            {
                return Fail(result);
            }
        }

        // Add only the roles that are in the new roles but not in currentRoles
        if (toAdd.Any())
        {
            var result = await userManager.AddClaimsAsync(user, toAdd.Select(x=>new Claim(type, x)));
            if (!result.Succeeded)
            {
                return Fail(result);
            }
        }
        return Result.Ok();
    }

    private async Task<Result> AssignRole(TUser user, IEnumerable<string> list )
    {
        string[] roles = [..list];
        var currentRoles = await userManager.GetRolesAsync(user);

        // Calculate roles to be removed and added
        var rolesToRemove = currentRoles.Except(roles).ToArray();
        var rolesToAdd = roles.Except(currentRoles).ToArray();

        // Remove only the roles that are in currentRoles but not in the new roles
        if (rolesToRemove.Any())
        {
            var result = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!result.Succeeded)
            {
                return Fail(result);
            }
        }

        // Add only the roles that are in the new roles but not in currentRoles
        if (rolesToAdd.Any())
        {
            var result = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!result.Succeeded)
            {
                return Fail(result);
            }
        }
        return Result.Ok();
    }

 
    private async Task<Result> AddClaimsToRole(TRole role,  IList<Claim> claims, string type, string[] values )
    {
        var currentValues = claims.Where(x => x.Type == type).Select(x => x.Value).ToArray();
        // Calculate roles to be removed and added
        var toRemove = currentValues.Except(values).ToArray();
        var toAdd = values.Except(currentValues).ToArray();

        // Remove only the roles that are in currentRoles but not in the new roles
        foreach (var claim in toRemove.Select(x=>new Claim(type,x)))
        {
            var identityResult = await roleManager.RemoveClaimAsync(role, claim);
            if (!identityResult.Succeeded)
            {
                return Fail(identityResult);
            }
        }

        foreach (var claim in toAdd.Select(x => new Claim(type, x)))
        {
            var identityResult = await roleManager.AddClaimAsync(role, claim);
            if (!identityResult.Succeeded)
            {
                return Fail(identityResult);
            }
        }
        return Result.Ok();
    }
    
    private async Task<Result> EnsureRoles(string[] roles)
    {
        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var res = await roleManager.CreateAsync(new TRole { Name = roleName });
            if (!res.Succeeded)
            {
                return Fail(res);
            }
        }

        return Result.Ok();
    }
    
    private Result Fail(IdentityResult result) =>
        Result.Fail(string.Join("\r\n", result.Errors.Select(e => e.Description)));

    private void Assure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new ResultException(string.Join("\r\n", result.Errors.Select(e => e.Description)));
        }
    } 
}