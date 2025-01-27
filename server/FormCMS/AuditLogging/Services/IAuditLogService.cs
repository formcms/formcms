using FormCMS.AuditLogging.Models;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.AuditLogging.Services;

public interface IAuditLogService
{
    Task<ListResponse> List(StrArgs args, int? offset,int? limit, CancellationToken ct = default);
    Task AddLog(ActionType actionType, string entityName, string id, Record record);
    Task EnsureAuditLogTable();
    XEntity GetAuditLogEntity();
}