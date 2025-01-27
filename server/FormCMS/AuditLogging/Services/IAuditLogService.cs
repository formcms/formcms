using FormCMS.AuditLogging.Models;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.EntityDisplayModel;

namespace FormCMS.AuditLogging.Services;

public interface IAuditLogService
{
    Task<ListResponse> List(int? offset,int? limit, CancellationToken ct = default);
    Task AddLog(ActionType actionType, string entityName, string id, Record record);
    Task EnsureAuditLogTable();
    XEntity GetAuditLogEntity();
}