using FormCMS.AuditLogging.Models;
using FormCMS.Core.HookFactory;

namespace FormCMS.AuditLogging.Services;

public interface IAuditLogService
{

    Task AddLog(ActionType actionType, string entityName, string id, Record record);
    Task EnsureAuditLogTable();
}