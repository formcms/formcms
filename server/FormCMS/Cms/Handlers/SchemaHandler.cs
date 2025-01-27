using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.EntityDisplayModel;
using FormCMS.Utils.ResultExt;
using DataType = FormCMS.Utils.EntityDisplayModel.DataType;
using DisplayType = FormCMS.Utils.EntityDisplayModel.DisplayType;
using Entity = FormCMS.Core.Descriptors.Entity;

namespace FormCMS.Cms.Handlers;

public static class SchemaHandler
{
    public static void MapSchemaHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/", async (
            ISchemaService svc, string type, CancellationToken ct
        ) => await svc.AllWithAction(type.ToEnum<SchemaType>() , ct));

        app.MapPost("/",  (
            ISchemaService schemaSvc, IEntitySchemaService entitySchemaSvc, Schema dto, CancellationToken ct
        ) => dto.Type switch
        {
            SchemaType.Entity =>  entitySchemaSvc.Save(dto, ct),
            _ =>  schemaSvc.SaveWithAction(dto, ct)
        });

        app.MapGet("/{id}", async (
            ISchemaService svc, int id, CancellationToken ct
        ) => await svc.ByIdWithAction(id, ct) ?? throw new ResultException($"Cannot find schema {id}"));

        app.MapGet("/name/{name}", async (
                ISchemaService svc, string name, string type, CancellationToken ct
            ) => await svc.GetByNameDefault(name, type.MustToEnum<SchemaType>(), ct) ??
                 throw new ResultException($"Cannot find schema {name} of type {type}"));

        app.MapDelete("/{id:int}", async (
            ISchemaService schemaSvc,
            IEntitySchemaService entitySchemaSvc,
            IQuerySchemaService querySchemaSvc,
            int id,
            CancellationToken ct
        ) =>
        {
            var schema = await schemaSvc.ById(id, ct);
            var task = schema?.Type switch
            {
                SchemaType.Entity => entitySchemaSvc.Delete(schema, ct),
                SchemaType.Query => querySchemaSvc.Delete(schema, ct),
                _ => schemaSvc.Delete(id, ct)
            };
            await task;
        });

        app.MapPost("/entity/define",  (
            IEntitySchemaService svc, Schema dto, CancellationToken ct
        ) =>  svc.SaveTableDefine(dto, ct));

        app.MapGet("/entity/{table}/define",  (
            IEntitySchemaService svc, string table, CancellationToken ct
        ) =>  svc.GetTableDefine(table, ct));

        app.MapGet("/entity/{name}", async (
            IEntitySchemaService service, string name, CancellationToken ct
        ) =>
        {
            var entity = await service.LoadEntity(name, ct).Ok();
            return entity.ToXEntity();
        });

        app.MapPost("/entity/add_or_update", async (
            IEntitySchemaService svc,
            Entity entity,
            CancellationToken ct
        ) => await svc.AddOrUpdateByName(entity, ct));

        app.MapGet("/graphql", (
            IQuerySchemaService service
        ) => Results.Redirect(service.GraphQlClientUrl()));
    }

    private static XEntity ToXEntity(this LoadedEntity entity)
        => new(
            Attributes: entity.Attributes.Select(x => x.ToXAttr()).ToArray(),
            Name: entity.Name,
            PrimaryKey: entity.PrimaryKey,
            DisplayName: entity.DisplayName,
            LabelAttributeName: entity.LabelAttributeName,
            DefaultPageSize: entity.DefaultPageSize
        );

    private static XAttr ToXAttr(this LoadedAttribute attribute)
    {
        return new(
            Field: attribute.Field,
            Header: attribute.Header,
            DataType: Enum.Parse<DataType>(attribute.DataType.ToString()),
            DisplayType: Enum.Parse<DisplayType>(attribute.DisplayType.ToString()),
            InList: attribute.InList,
            InDetail: attribute.InDetail,
            IsDefault: attribute.IsDefault,
            Options: attribute.Options,
            Junction: attribute.Junction?.TargetEntity.ToXEntity(),
            Lookup: attribute.Lookup?.TargetEntity.ToXEntity(),
            Collection: attribute.Collection?.TargetEntity.ToXEntity()
        );
    }
}