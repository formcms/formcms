using FormCMS.AuditLogging.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.AuditLogging.Handlers;

public static class AuditLogHandlers
{
    public static void MapAuditLogHandlers(this RouteGroupBuilder builder)
    {
        builder.MapGet("/counts", (
            int n,
            IAuditLogService auditLogService,
            CancellationToken ct
        ) => auditLogService.GetActionCounts(n,ct));
        
        builder.MapGet("/", (
            IAuditLogService s, 
            HttpContext context,
            int? offset, 
            int? limit, 
            CancellationToken ct
        ) => s.List(QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, ct));

        builder.MapGet("/{id}", (
            IAuditLogService s,
            int id,
            CancellationToken ct
        ) => s.Single(id, ct));
        
        builder.MapGet("/entity", (IAuditLogService s) => s.GetAuditLogEntity());
    }
}