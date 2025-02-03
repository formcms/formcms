using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using DisplayType = FormCMS.Utils.DisplayModels.DisplayType;
using Entity = FormCMS.Core.Descriptors.Entity;

namespace FormCMS.Cms.Handlers;

public static class SchemaHandler
{
    public static void MapSchemaHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/", async (
            ISchemaService svc, string type, CancellationToken ct
        ) => await svc.AllWithAction(type.ToEnum<SchemaType>() , ct));

       

        app.MapGet("/{id}", async (
            ISchemaService svc, int id, CancellationToken ct
        ) => await svc.ByIdWithAction(id, ct) ?? throw new ResultException($"Cannot find schema {id}"));

        app.MapGet("/menu/{name}", async (
                ISchemaService svc, string name, CancellationToken ct
            ) =>
        {
            var schema = await svc.GetByNameDefault(name, SchemaType.Menu, ct) ??
                   throw new ResultException($"Cannot find menu [{name}]");
            return schema.Settings.Menu;
        });

       

      
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

        app.MapGet("/graphql", (
            IQuerySchemaService service
        ) => Results.Redirect(service.GraphQlClientUrl()));

        
        app.MapPost("/",  (
            ISchemaService schemaSvc, IEntitySchemaService entitySchemaSvc, Schema dto, CancellationToken ct
        ) => dto.Type switch
        {
            SchemaType.Entity =>  entitySchemaSvc.Save(dto, ct),
            _ =>  schemaSvc.SaveWithAction(dto, ct)
        });
        
        app.MapPost("/entity/define", (
            IEntitySchemaService svc, Schema dto, CancellationToken ct
        ) => svc.SaveTableDefine(dto, ct));

        app.MapPost("/entity/add_or_update", async (
            IEntitySchemaService svc,
            Entity entity,
            CancellationToken ct
        ) => await svc.AddOrUpdateByName(entity, ct));
        
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
       
    }

    private static XEntity ToXEntity(this LoadedEntity entity)
        => new(
            Attributes: entity.Attributes.Select(x => x.ToXAttr()).ToArray(),
            Name: entity.Name,
            PrimaryKey: entity.PrimaryKey,
            DisplayName: entity.DisplayName,
            LabelAttributeName: entity.LabelAttributeName,
            DefaultPageSize: entity.DefaultPageSize,
            PreviewUrl:entity.PreviewUrl
            
        );

    private static XAttr ToXAttr(this LoadedAttribute attribute)
    {
        return new(
            Field: attribute.Field,
            Header: attribute.Header,
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