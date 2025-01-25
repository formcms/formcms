using FormCMS.Core.Descriptors;
using FormCMS.Utils.DictionaryExt;
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

public enum AuditLogFields
{
    Id,
    UserId,
    UserName,
    Action,
    EntityName,
    RecordId,
    Payload,
    CreatedAt
}

public static class AuditLogConstants
{
    public const string TableName = "__auditlog";
}

public static class AuditLogHelper
{
    public static Query Insert(this AuditLog auditLog)
        => new Query(AuditLogConstants.TableName)
            .AsInsert(
                DictionaryExt.FormObject(auditLog, blackList: [AuditLogFields.Id, AuditLogFields.CreatedAt])
            );

    public static Query List(Filter[] filters, Sort[] sorts, Pagination pagination)
    {
        return new Query(AuditLogConstants.TableName);
    }
}