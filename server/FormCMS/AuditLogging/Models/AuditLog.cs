using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using Query = SqlKata.Query;

namespace FormCMS.AuditLogging.Models;

public enum ActionType
{
    Create,
    Update,
    Delete
}

public record AuditLog(
    int Id,
    string UserId,
    string UserName,
    ActionType Action,
    string EntityName,
    string RecordId,
    Record Payload,
    DateTime CreatedAt
);

public static class AuditLogConstants
{
    public const string TableName = "__auditlog";
    public const int DefaultPageSize = 50;
}

public static class AuditLogHelper
{
    public static readonly XEntity Entity  = XEntityExtensions.CreateEntity<AuditLog>(
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

    public static readonly Column[] Columns =  [
        ColumnHelper.CreateCamelColumn<AuditLog,int>(x=>x.Id),
        ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.UserName),
        ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.UserId),
        ColumnHelper.CreateCamelColumn<AuditLog>(x=>x.Action,ColumnType.String),
        ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.EntityName),
        ColumnHelper.CreateCamelColumn<AuditLog,string>(x=>x.RecordId),
        ColumnHelper.CreateCamelColumn<AuditLog,DateTime>(x=>x.CreatedAt),
        ColumnHelper.CreateCamelColumn<AuditLog>(x=>x.Payload,ColumnType.Text),
    ];

    public static Query Insert(this AuditLog auditLog) =>
        new Query(AuditLogConstants.TableName)
            .AsInsert(RecordExtensions.FormObject(
                auditLog,
                blackList: [nameof(AuditLog.Id), nameof(AuditLog.CreatedAt)]
            ));

    public static Query List(int?offset = null, int? limit = null)
    {
        var q = new Query(AuditLogConstants.TableName);
        q= q.Select(Entity.Attributes.Where(x=>x.InList).Select(x=>x.Field));
        if (offset > 0) q.Offset(offset.Value);
        q.Limit(limit ?? AuditLogConstants.DefaultPageSize);
        return q;
    }

    public static Query Count()
    {
        var q = new Query(AuditLogConstants.TableName);
        return q;
    }
    
}