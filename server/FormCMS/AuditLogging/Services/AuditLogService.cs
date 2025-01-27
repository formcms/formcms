using FormCMS.AuditLogging.Models;
using FormCMS.CoreKit.UserContext;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpContextExt;

namespace FormCMS.AuditLogging.Services;

public class AuditLogService(
    IHttpContextAccessor httpContextAccessor,
    KateQueryExecutor executor,
    IRelationDbDao dao
    ):IAuditLogService
{
    public async Task<ListResponse> List(StrArgs args,int? offset, int? limit, CancellationToken ct)
    {
        var items = await executor.Many(AuditLogHelper.List([], [], offset,limit),ct);
        var count = await executor.Count(AuditLogHelper.Count([]),ct);
        return new ListResponse(items,count);
    }
    
     public  Task AddLog(ActionType actionType, string entityName, string id ,Record record)
        {
            var log = new AuditLog(
                Id: 0,
                UserId: httpContextAccessor.HttpContext.GetUserId(),
                UserName:httpContextAccessor.HttpContext.GetUserName(),
                ActionType.Create,
                EntityName:entityName,
                RecordId: id,
                Payload:record,
                CreatedAt: DateTime.Now
            );
            return executor.ExecInt(log.Insert());
        }
    public async Task EnsureAuditLogTable()
    {
        var cols = await dao.GetColumnDefinitions(AuditLogConstants.TableName,CancellationToken.None);
        if (cols.Length > 0)
        {
            return;
        }
        await dao.CreateTable(AuditLogConstants.TableName, AuditLogHelper.Columns, CancellationToken.None);
    }

    public XEntity GetAuditLogEntity() => AuditLogHelper.Entity;
}