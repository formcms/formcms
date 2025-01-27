using System.Collections.Immutable;
using System.Security.Claims;
using FormCMS.Auth.DTO;
using FluentResults;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.HttpContextExt;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Descriptors_Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Auth.Services;

public class SchemaPermissionService<TUser>(
    IHttpContextAccessor contextAccessor,
    SignInManager<TUser> signInManager,
    UserManager<TUser> userManager,
    ISchemaService schemaService
) :ISchemaPermissionService
    where TUser : IdentityUser, new()
{
   
    public string[] GetAll()
    {
        if (!contextAccessor.HttpContext.HasRole(RoleConstants.Sa) &&
            !contextAccessor.HttpContext.HasRole(RoleConstants.Admin))
        {
            throw new ResultException($"Fail to get schema list, you don't have [Sa] or [Admin] role.");
        }
        return [];
    }

    public void GetOne(Schema schema)
    {
        if (!contextAccessor.HttpContext.HasRole(RoleConstants.Sa) &&
            !contextAccessor.HttpContext.HasRole(RoleConstants.Admin))
        {
            throw new ResultException($"You don't have permission to access {schema.Type}:{schema.Name}");
        }
    }

    public async Task Delete(int schemaId)
    {
        var find = await schemaService.ById(schemaId) ??
                   throw new ResultException($"can not find schema by id [{schemaId}]");
        await EnsureWritePermissionAsync(find);
    }

    public async Task<Schema> BeforeSave(Schema schema)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
        {
            throw new ResultException($"You are not logged in, can not save schema {schema.Type} [{schema.Name}]");
        }
        await EnsureWritePermissionAsync(schema);

        if (schema.Id == 0)
        {
            schema = schema with { CreatedBy = userId };
            if (schema.Type == SchemaType.Entity)
            {
                schema = EnsureSchemaHaveCreatedByField(schema).Ok();
            }
        }

        return schema;
    }

    public async Task AfterSave(Schema schema)
    {
        await EnsureCurrentUserHaveEntityAccess(schema);
    }

    private async Task EnsureWritePermissionAsync(Schema schema)
    {
        if (contextAccessor.HttpContext?.User.Identity?.IsAuthenticated == false)
        {
            throw new ResultException($"You are not logged in, can not save {schema.Type} [{schema.Name}]");
        }
        var hasPermission = schema.Type switch
        {
            SchemaType.Menu => contextAccessor.HttpContext.HasRole(RoleConstants.Sa),
            _ when schema.Id is 0 => 
                contextAccessor.HttpContext.HasRole(RoleConstants.Admin) || 
                contextAccessor.HttpContext.HasRole(RoleConstants.Sa),
            _ => 
                contextAccessor.HttpContext.HasRole(RoleConstants.Sa) || 
                await IsCreatedByCurrentUser(schema)
        };

        if (!hasPermission)
        {
            throw new ResultException($"You don't have permission to save {schema.Type} [{schema.Name}]");
        }
    }

    private async Task EnsureCurrentUserHaveEntityAccess(Schema schema)
    {
        var user = await userManager.GetUserAsync(contextAccessor.HttpContext!.User) ??
                   throw new Exception("User not found.");

        var claims = await userManager.GetClaimsAsync(user);

        var hasAccess = claims.Any(claim => 
            claim.Value == schema.Name && 
            claim.Type is AccessScope.RestrictedAccess or AccessScope.FullAccess
        );

        if (!hasAccess)
        {
            await userManager.AddClaimAsync(user, new Claim(AccessScope.RestrictedAccess, schema.Name));
            await signInManager.RefreshSignInAsync(user);
        }
    }

    private static Result<Schema> EnsureSchemaHaveCreatedByField(Schema schema)
    {
        var entity = schema.Settings.Entity;
        if (entity is null) return Result.Fail("can not ensure schema have created_by field, invalid Entity payload");
        if (schema.Settings.Entity?.Attributes.FirstOrDefault(x=>x.Field == Constants.CreatedBy) is not null) return schema;

        ImmutableArray<Descriptors_Attribute> attributes =
        [
            ..entity.Attributes,
            new(
                Field: Constants.CreatedBy, Header: Constants.CreatedBy, DataType: DataType.String,
                InList: false, InDetail: false, IsDefault: true
            )
        ];
        return schema with{Settings = new Settings(Entity: entity with{Attributes = attributes})};
    }

    private async Task<bool> IsCreatedByCurrentUser(Schema schema)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
            throw new ResultException("Can not verify schema is created by you, you are not logged in.");
        var find = await schemaService.ById(schema.Id)
            ?? throw new ResultException($"Can not verify schema is created by you, can not find schema by id [{schema.Id}]");
        return find.CreatedBy == userId;
    }
}
  
