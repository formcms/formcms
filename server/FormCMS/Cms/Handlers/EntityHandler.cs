using System.Text.Json;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpContextExt;

namespace FormCMS.Cms.Handlers;

public static class EntityHandler
{
    public static void MapEntityHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/{name}",  (
            IEntityService entityService,
            HttpContext context,
            string name,
            string? offset,
            string? limit,
            string? mode,
            CancellationToken ct
        ) =>  entityService.ListWithAction(
            name,
            mode?.ToEnum<ListResponseMode>()??ListResponseMode.All, 
            new Pagination(offset, limit),
            context.Args(),
            ct));
        
        app.MapGet("/tree/{name}",  (
            IEntityService entityService,
            string name,
            CancellationToken ct
        ) =>  entityService.ListAsTree( name, ct));
        

        app.MapGet("/{name}/{id}",  (
            IEntityService entityService,
            string name,
            string id,
            CancellationToken ct
        ) =>  entityService.SingleWithAction(name, id, ct));

        app.MapPost("/{name}/insert",  (
            IEntityService entityService,
            string name,
            JsonElement ele,
            CancellationToken ct
        ) =>  entityService.InsertWithAction(name, ele, ct));

        app.MapPost("/{name}/update",  (
            IEntityService entityService,
            string name,
            JsonElement ele,
            CancellationToken ct
        ) =>  entityService.UpdateWithAction(name, ele, ct));

      
        app.MapPost("/{name}/delete",  (
            IEntityService entityService,
            string name,
            JsonElement ele,
            CancellationToken ct
        ) =>  entityService.DeleteWithAction(name, ele, ct));
        
         
        app.MapPost("/{name}/publication", (
            IEntityService entityService,
            string name,
            JsonElement ele,
            CancellationToken ct
        ) => entityService.SavePublicationSettings(name, ele, ct));
       
        app.MapPost("/junction/{name}/{id}/{attributeName}/delete",  (
            IEntityService entityService,
            string name,
            string id,
            string attributeName,
            JsonElement[] items,
            CancellationToken ct
        ) =>  entityService.JunctionDelete(name, id, attributeName, items, ct));

        app.MapPost("/junction/{name}/{id}/{attr}/save",  (
            IEntityService entityService,
            string name,
            string id,
            string attr,
            JsonElement[] elements,
            CancellationToken ct
        ) =>  entityService.JunctionSave(name, id, attr, elements, ct));
            
        app.MapGet("/junction/target_ids/{name}/{id}/{attr}",  (
            IEntityService entityService,
            string name,
            string id,
            string attr,
            CancellationToken ct
        ) =>  entityService.JunctionTargetIds( name, id, attr, ct));

        app.MapGet("/junction/{name}/{id}/{attr}",  (
            IEntityService entityService,
            HttpContext context,
            string name,
            string id,
            string attr,
            string? offset,
            string? limit,
            bool exclude,
            CancellationToken ct
        ) =>  entityService.JunctionList(
            name, id, attr, exclude,
            new Pagination(offset, limit),
            context.Args(),
            ct));

        app.MapGet("/collection/{name}/{id}/{attr}",  (
            IEntityService entityService,
            HttpContext context,
            string name,
            string id,
            string attr,
            string? offset,
            string? limit,
            CancellationToken ct
        ) =>  entityService.CollectionList(
            name, id, attr,
            new Pagination(offset, limit),
            context.Args(),
            ct));

        app.MapPost("/collection/{name}/{id}/{attr}/insert",  (
            IEntityService entityService,
            string name,
            string id,
            string attr,
            JsonElement element,
            CancellationToken ct
        ) =>  entityService.CollectionInsert(name, id, attr, element, ct));

        app.MapGet("/lookup/{name}",  (
            IEntityService entityService,
            string name,
            string? query,
            CancellationToken ct
        ) =>  entityService.LookupList(name, query ?? "", ct));
    }
}