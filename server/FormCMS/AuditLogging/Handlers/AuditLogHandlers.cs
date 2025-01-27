using FormCMS.AuditLogging.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.AuditLogging.Handlers;

public static class AuditLogHandlers
{
    public static void MapAuditLogHandlers(this RouteGroupBuilder builder)
    {
        builder.MapGet("/entity", (IAuditLogService s) => s.GetAuditLogEntity());
        builder.MapGet("/", (
            IAuditLogService s, 
            HttpContext context,
            int? offset, int? limit, 
            CancellationToken ct
        ) => s.List(QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, ct));
    }
}