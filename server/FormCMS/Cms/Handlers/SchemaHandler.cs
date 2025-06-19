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
            var schema = await svc.ByNameOrDefault(name,schemaType ,null, ct) ??
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
            ISchemaService schemaSvc,
            IEntitySchemaService entitySchemaSvc,
            Schema dto,
            bool? publish,
            CancellationToken ct
        ) => dto.Type switch
        {
            SchemaType.Entity => entitySchemaSvc.Save(dto,publish??false, ct),
            _ => schemaSvc.SaveWithAction(dto,publish??false, ct)
        });
        
        
        app.MapPost("/entity/define", (
            IEntitySchemaService svc, 
            Schema dto, 
            bool? publish,
            CancellationToken ct
        ) => svc.SaveTableDefine(dto,publish??false, ct));

        app.MapPost("/entity/add_or_update", async (
            IEntitySchemaService svc,
            Entity entity,
            bool? publish,
            CancellationToken ct
        ) => await svc.AddOrUpdateByName(entity,publish??false, ct));

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




   
}