using FormCMS.AuditLogging.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.EntityDisplayModel;
using FormCMS.Utils.IdentityExt;

namespace FormCMS.AuditLogging.Services;

public class AuditLogService(
    IHttpContextAccessor httpContextAccessor,
    KateQueryExecutor executor,
    IRelationDbDao dao
    ):IAuditLogService
{
    public async Task<ListResponse> List(int? offset, int? limit, CancellationToken ct)
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

        cols =
        [
            ColumnHelper.CreateCamelColumn<AuditLog,int>(x=>x.Id),
            ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.UserName),
            ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.UserId),
            ColumnHelper.CreateCamelColumn<AuditLog>(x=>x.Action,ColumnType.String),
            ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.EntityName),
            ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.RecordId),
            ColumnHelper.CreateCamelColumn<AuditLog,DateTime>(x=>x.CreatedAt),
            ColumnHelper.CreateCamelColumn<AuditLog>(x=>x.Payload,ColumnType.Text),
        ];
        
        await dao.CreateTable(AuditLogConstants.TableName, cols, CancellationToken.None);
    }

    public XEntity GetAuditLogEntity()
        => XEntityExtensions.CreateEntity<AuditLog>(
            nameof(AuditLog.EntityName),
            defaultPageSize: AuditLogConstants.DefaultPageSize,
            attributes:
            [
                XAttrExtensions.CreateAttr<AuditLog, int>(x => x.Id, isDefault: true),
                XAttrExtensions.CreateAttr<AuditLog, string>(x => x.UserId),
                XAttrExtensions.CreateAttr<AuditLog, string>(x => x.UserName),
                XAttrExtensions.CreateAttr<AuditLog, string>(x => x.EntityName),
                XAttrExtensions.CreateAttr<AuditLog, object>(x => x.Action),
                XAttrExtensions.CreateAttr<AuditLog, string>(x => x.RecordId),
                XAttrExtensions.CreateAttr<AuditLog, DateTime>(x => x.CreatedAt,isDefault:true),
                XAttrExtensions.CreateAttr<AuditLog, object>(x => x.Payload, inList: false)
            ]);
}