using FormCMS.Activities.Services;

namespace FormCMS.Activities.Handlers;

public static class ActivityHandler
{
    public static void MapActivityHandler(this RouteGroupBuilder builder)
    {
        builder.MapGet("/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            IActivityService s,
            CancellationToken ct
        ) => s.Get(entityName, recordId, ct));

        builder.MapPost("/toggle/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            string type,
            bool active,
            IActivityService s,
            CancellationToken ct
        ) => s.Toggle(entityName, recordId, type, active, ct));
        
        builder.MapPost("/record/{entityName}/{recordId:long}", (
            string entityName,
            long recordId,
            string type,
            IActivityService s,
            CancellationToken ct
        ) => s.Record(entityName, recordId, type, ct));
    }
}