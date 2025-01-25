using FormCMS.AuditLogging.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.IdentityExt;

namespace FormCMS.AuditLogging.Services;

public class AuditLogService(
    IHttpContextAccessor httpContextAccessor,
    KateQueryExecutor executor,
    IRelationDbDao dao
    ):IAuditLogService
{
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

        cols =
        [
            AuditLogFields.Id.CreateColumn(ColumnType.Int),
            AuditLogFields.UserName.CreateColumn(ColumnType.String),
            AuditLogFields.UserId.CreateColumn(ColumnType.String),
            AuditLogFields.Action.CreateColumn(ColumnType.String),
            AuditLogFields.EntityName.CreateColumn(ColumnType.String),
            AuditLogFields.RecordId.CreateColumn(ColumnType.String),
            AuditLogFields.Payload.CreateColumn(ColumnType.Text),
            AuditLogFields.CreatedAt.CreateColumn(ColumnType.Datetime),
        ];
        
        await dao.CreateTable(AuditLogConstants.TableName, cols, CancellationToken.None);
    }
}