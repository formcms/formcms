using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Services;

public class AdminPanelSchemaService(
    ISchemaService svc,
    IEntitySchemaService entitySchemaService,
    IProfileService profileService
) : IAdminPanelSchemaService
{
    public async Task<IResult> GetMenu(string name, CancellationToken ct)
    {
        if (profileService.GetInfo()?.CanAccessAdmin != true) return Results.Unauthorized();
        var schema = await svc.GetByNameDefault(name, SchemaType.Menu, null, ct) ??
                     throw new ResultException($"Cannot find menu [{name}]");
        return Results.Ok(schema.Settings.Menu);
    }

    public async Task<IResult> GetEntity(string name, CancellationToken ct)
    {
        if (profileService.GetInfo()?.CanAccessAdmin != true) return Results.Unauthorized();
        var entity = await entitySchemaService.LoadEntity(name, null, ct).Ok();
        return Results.Ok(ToXEntity(entity));
    }

    private static XEntity? ToXEntity(LoadedEntity? entity)
        => entity is null
            ? null
            : new(
                Attributes: entity.Attributes.Select(ToXAttr).ToArray(),
                Name: entity.Name,
                PrimaryKey: entity.PrimaryKey,
                DisplayName: entity.DisplayName,
                LabelAttributeName: entity.LabelAttributeName,
                DefaultPageSize: entity.DefaultPageSize,
                PreviewUrl: entity.PreviewUrl
            );

    private static XAttr ToXAttr(LoadedAttribute attribute)
    {
        return new(
            Field: attribute.Field,
            Header: attribute.Header,
            DisplayType: Enum.Parse<DisplayType>(attribute.DisplayType.ToString()),
            InList: attribute.InList,
            InDetail: attribute.InDetail,
            IsDefault: attribute.IsDefault,
            Options: attribute.Options,
            Junction: ToXEntity(attribute.Junction?.TargetEntity),
            Lookup: ToXEntity(attribute.Lookup?.TargetEntity),
            Collection: ToXEntity(attribute.Collection?.TargetEntity)
        );
    }
}