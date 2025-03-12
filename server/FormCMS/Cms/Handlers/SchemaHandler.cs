using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using DisplayType = FormCMS.Utils.DisplayModels.DisplayType;
using Entity = FormCMS.Core.Descriptors.Entity;

namespace FormCMS.Cms.Handlers;

public static class SchemaHandler
{
    public static RouteGroupBuilder MapSchemaBuilderSchemaHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/", async (
            ISchemaService svc, string type, string? status, CancellationToken ct
        ) => await svc.AllWithAction(type.ToEnum<SchemaType>() ,status?.ToEnum<PublicationStatus>(), ct));

        app.MapGet("/{id}", async (
            ISchemaService svc, int id, CancellationToken ct
        ) => await svc.ByIdWithAction(id, ct) ?? throw new ResultException($"Cannot find schema {id}"));

        app.MapGet("/history/{schemaId}", (
            ISchemaService svc, string schemaId, CancellationToken ct
        ) => svc.History(schemaId, ct));
       
        app.MapGet("/name/{name}", async (
            ISchemaService svc, string name, string type, CancellationToken ct
        ) =>
        {
            var schemaType = Enum.Parse<SchemaType>(type, true);
            var schema = await svc.GetByNameDefault(name,schemaType ,null, ct) ??
                         throw new ResultException($"Cannot find menu [{name}]");
            return schema;
        });
        
        app.MapGet("/entity/{table}/define", (
            IEntitySchemaService svc, string table, CancellationToken ct
        ) => svc.GetTableDefine(table, ct));

        app.MapGet("/graphql", (
            IQuerySchemaService service
        ) => Results.Redirect(service.GraphQlClientUrl()));
        
        app.MapPost("/", (
            ISchemaService schemaSvc, IEntitySchemaService entitySchemaSvc, Schema dto, CancellationToken ct
        ) => dto.Type switch
        {
            SchemaType.Entity => entitySchemaSvc.Save(dto, ct),
            _ => schemaSvc.SaveWithAction(dto, ct)
        });
        
        
        app.MapPost("/entity/define", (
            IEntitySchemaService svc, Schema dto, CancellationToken ct
        ) => svc.SaveTableDefine(dto, ct));

        app.MapPost("/entity/add_or_update", async (
            IEntitySchemaService svc,
            Entity entity,
            CancellationToken ct
        ) => await svc.AddOrUpdateByName(entity, ct));

        app.MapPost("/publish", (
            ISchemaService svc,
            Schema schema,
            CancellationToken ct
        ) => svc.Publish(schema, ct));
        
        app.MapDelete("/{id}", async (
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

        return app;
    }

    //these two APIs are availabe for any login user
    public static RouteGroupBuilder MapAdminPanelSchemaHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/menu/{name}", async (
            ISchemaService svc, 
            IProfileService profileService, 
            string name, 
            CancellationToken ct
        ) =>
        {
            if (profileService.GetInfo() is null) return Results.Unauthorized();
            var schema = await svc.GetByNameDefault(name, SchemaType.Menu, null, ct) ??
                         throw new ResultException($"Cannot find menu [{name}]");
            return Results.Ok(schema.Settings.Menu);
        });

        app.MapGet("/entity/{name}", async (
            IEntitySchemaService service, 
            IProfileService profileService, 
            string name, 
            CancellationToken ct
        ) =>
        {
            if (profileService.GetInfo() is null) return Results.Unauthorized();
            var entity = await service.LoadEntity(name, null, ct).Ok();
            return Results.Ok(entity.ToXEntity());
        });
        return app;
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