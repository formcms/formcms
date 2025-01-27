using FormCMS.AuditLogging.Services;

namespace FormCMS.AuditLogging.Handlers;

public static class AuditLogHandlers
{
    public static void MapAuditLogHandlers(this RouteGroupBuilder builder)
    {
        builder.MapGet("/entity", (IAuditLogService s) => s.GetAuditLogEntity());
        builder.MapGet(
            "/",
            (
                IAuditLogService s, int? offset, int? limit, CancellationToken ct
            ) => s.List(offset, limit, ct));
    }
}