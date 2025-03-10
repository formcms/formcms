using System.Collections.Immutable;
using FluentResults;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.Utils.ResultExt;
using Descriptors_Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Auth.Services;

public class SchemaAuthService(
    IProfileService profileService,
    ISchemaService schemaService
) :ISchemaAuthService
{

    public void GetAll()
    {
        var access = profileService.GetInfo();
        if (!access.HasRole(Roles.Sa) && !access.HasRole(Roles.Admin))
        {
            throw new ResultException($"Fail to get schema list, you don't have [Sa] or [Admin] role.");
        }
    }

    public void GetOne(Schema schema)
    {
        var access = profileService.GetInfo();
        if (!access.HasRole(Roles.Sa) && !access.HasRole(Roles.Admin))
        {
            throw new ResultException($"You don't have permission to access {schema.Type}:{schema.Name}");
        }
    }

    public async Task Delete(Schema schema)
    {
        await EnsureWritePermissionAsync(schema);
    }

    public async Task<Schema> BeforeSave(Schema schema)
    {
        var access = profileService.GetInfo();
        await EnsureWritePermissionAsync(schema);

        schema = schema with { CreatedBy = access?.Id ??"" };
        if (schema.Type == SchemaType.Entity)
        {
            schema = EnsureSchemaHaveCreatedByField(schema).Ok();
        }

        return schema;
    }

    public async Task AfterSave(Schema schema)
    {
        if (schema.Type == SchemaType.Entity)
        {
            await profileService.EnsureCurrentUserHaveEntityAccess(schema.Name);
        }
    }

    private async Task EnsureWritePermissionAsync(Schema schema)
    {
        var access = profileService.GetInfo();
        var hasPermission = schema.Type switch
        {
            SchemaType.Menu => access.HasRole(Roles.Sa),
            _ when schema.Id is 0 => 
                access.HasRole(Roles.Admin) || 
                access.HasRole(Roles.Sa),
            _ => 
                access.HasRole(Roles.Sa) || 
                await IsCreatedByCurrentUser(schema)
        };

        if (!hasPermission)
        {
            throw new ResultException($"You don't have permission to save {schema.Type} [{schema.Name}]");
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
        var access = profileService.GetInfo();
        var find = await schemaService.ById(schema.Id)
            ?? throw new ResultException($"Can not verify schema is created by you, can not find schema by id [{schema.Id}]");
        return find.CreatedBy == access?.Id;
    }
}