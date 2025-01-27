using System.Security.Claims;
using FormCMS.Auth.DTO;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FormCMS.Utils.IdentityExt;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.Services;

public class AccountService<TUser, TRole,TCtx>(
    UserManager<TUser> userManager,
    RoleManager<TRole> roleManager,
    IHttpContextAccessor accessor,
    TCtx context,
    KateQueryExecutor queryExecutor
) : IAccountService
    where TUser : IdentityUser, new()
    where TRole : IdentityRole, new()
    where TCtx : IdentityDbContext<TUser>

{
    public async Task<string[]> GetEntities(CancellationToken ct)
    {
        var records= await queryExecutor.Many(SchemaHelper.ByNameAndType(SchemaType.Entity,null),ct);
        return records.Select(x => x.CamelKeyStr(nameof(Entity.Name))).ToArray();
    }
    
    public async Task<string[]> GetRoles(CancellationToken ct)
    {
        if (!accessor.HttpContext.HasRole(RoleConstants.Admin) && !accessor.HttpContext.HasRole(RoleConstants.Sa))
            throw new UnauthorizedAccessException();
        var roles = await context.Roles.Select(x => x.Name??"").ToArrayAsync(ct);
        return roles;
    }

    public async Task<UserDto> GetSingleUser(string id, CancellationToken ct)
    {
        if (!accessor.HttpContext.HasRole(RoleConstants.Admin) && !accessor.HttpContext.HasRole(RoleConstants.Sa))
            throw new UnauthorizedAccessException();
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
        return new UserDto
        (
            Email: item.Key.Email!,
            Id: item.Key.Id,
            Roles: [..item.Values.Where(x => x.role is not null).Select(x => x.role.Name!).Distinct()],
            ReadWriteEntities:
            [
                ..item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.FullAccess)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            RestrictedReadWriteEntities:
            [
                ..item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.RestrictedAccess)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            ReadonlyEntities:
            [
                ..item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.FullRead)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            RestrictedReadonlyEntities:
            [
                ..item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.RestrictedRead)
                    .Select(x => x.userClaim.ClaimValue!).Distinct()
            ],
            AllowedMenus: []
        );
    }

    public async Task<UserDto[]> GetUsers(CancellationToken ct)
    {
        if (!accessor.HttpContext.HasRole(RoleConstants.Admin) && !accessor.HttpContext.HasRole(RoleConstants.Sa))
            throw new UnauthorizedAccessException();

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
        return [..items.Select(x => new UserDto
        (
            Email : x.Key.Email!,
            Id : x.Key.Id,
            Roles : [..x.Roles.Where(val=>val?.role is not null).Select(val => val.role.Name!).Distinct()],
            AllowedMenus:[],
            ReadonlyEntities:[],
            ReadWriteEntities:[],
            RestrictedReadonlyEntities:[],
            RestrictedReadWriteEntities:[]
        ))];
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
            return Result.Fail(res.ErrorMessage());
        }

        res = await userManager.AddToRolesAsync(user, roles);
        return !res.Succeeded ? Result.Fail(res.ErrorMessage()) : Result.Ok();
    }

    public async Task DeleteUser(string id)
    {
        if(!accessor.HttpContext.HasRole(RoleConstants.Sa)) throw new ResultException("Only supper admin have permission");
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new ResultException($"Fail to Delete User, Cannot find user by id [{id}]");
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new ResultException(result.ErrorMessage());
        }
    }
    
   

    public async Task SaveUser(UserDto dto)
    {
        if (!accessor.HttpContext.HasRole(RoleConstants.Sa)) throw new ResultException("Only supper admin have permission");
        var user = await MustFindUser(dto.Id);
        var claims = await userManager.GetClaimsAsync(user);
        await AssignRole(user, dto.Roles).Ok();
        await AssignClaim(user, claims, AccessScope.FullAccess, dto.ReadWriteEntities).Ok();
        await AssignClaim(user, claims, AccessScope.RestrictedAccess, dto.RestrictedReadWriteEntities).Ok();
        await AssignClaim(user, claims, AccessScope.FullRead, dto.ReadonlyEntities).Ok();
        await AssignClaim(user, claims, AccessScope.RestrictedRead, dto.RestrictedReadonlyEntities).Ok();
    }
    
    public async Task<RoleDto> GetSingleRole(string name)
    {
        var role = await roleManager.FindByNameAsync(name) ?? throw new ResultException($"Cannot find role by name [{name}]");
        var claims = await roleManager.GetClaimsAsync(role);
        return new RoleDto
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
        if (name is RoleConstants.Admin or RoleConstants.Sa) throw new ResultException($"Cannot delete System Build-in Role [{name}]");
        if (!accessor.HttpContext.HasRole(RoleConstants.Sa)) throw new ResultException("Only supper admin have permission");
        var role = await roleManager.FindByNameAsync(name)?? throw new ResultException($"Cannot find role by name [{name}]");
        var result = (await roleManager.DeleteAsync(role));
        if (!result.Succeeded) throw new ResultException(result.ErrorMessage());
    }

    public async Task SaveRole(RoleDto roleDto)
    {
        if (!accessor.HttpContext.HasRole(RoleConstants.Sa)) throw new ResultException("Only supper admin have permission");
        if (string.IsNullOrWhiteSpace(roleDto.Name))
        {
            throw new ResultException("Role name can not be null");
        }
        await EnsureRoles([roleDto.Name]).Ok();
        var role = await roleManager.FindByNameAsync(roleDto.Name);
        var claims =await roleManager.GetClaimsAsync(role!);
        await AddClaimsToRole(role!, claims, AccessScope.FullAccess, roleDto.ReadWriteEntities).Ok();
        await AddClaimsToRole(role!, claims, AccessScope.RestrictedAccess, roleDto.RestrictedReadWriteEntities).Ok();
        await AddClaimsToRole(role!, claims, AccessScope.FullRead, roleDto.ReadonlyEntities).Ok();
        await AddClaimsToRole(role!, claims, AccessScope.RestrictedRead, roleDto.RestrictedReadonlyEntities).Ok();
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
                return Result.Fail(result.ErrorMessage());
            }
        }

        // Add only the roles that are in the new roles but not in currentRoles
        if (toAdd.Any())
        {
            var result = await userManager.AddClaimsAsync(user, toAdd.Select(x=>new Claim(type, x)));
            if (!result.Succeeded)
            {
                return Result.Fail(result.ErrorMessage());
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
                return Result.Fail(result.ErrorMessage());
            }
        }

        // Add only the roles that are in the new roles but not in currentRoles
        if (rolesToAdd.Any())
        {
            var result = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!result.Succeeded)
            {
                return Result.Fail(result.ErrorMessage());
            }
        }
        return Result.Ok();
    }

 
    private async Task<Result> AddClaimsToRole(TRole role,  IList<Claim> claims, string type, string[] values )
    {
        values ??= [];
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
                return Result.Fail(identityResult.ErrorMessage());
            }
        }

        foreach (var claim in toAdd.Select(x => new Claim(type, x)))
        {
            var identityResult = await roleManager.AddClaimAsync(role, claim);
            if (!identityResult.Succeeded)
            {
                return Result.Fail(identityResult.ErrorMessage());
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
                return Result.Fail(res.ErrorMessage());
            }
        }

        return Result.Ok();
    }
    
}