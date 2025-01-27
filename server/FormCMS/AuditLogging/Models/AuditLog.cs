using FormCMS.Core.Descriptors;
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
    private static readonly string []ListAttributes =
    [
        nameof(AuditLog.Id),
        nameof(AuditLog.UserId),
        nameof(AuditLog.UserName),
        nameof(AuditLog.Action),
        nameof(AuditLog.EntityName),
        nameof(AuditLog.RecordId),
        nameof(AuditLog.CreatedAt),
    ];

    public static string[] AllAttributes =
    [
        ..ListAttributes,
        nameof(AuditLog.Payload),
    ];

    public static Query Insert(this AuditLog auditLog) =>
        new Query(AuditLogConstants.TableName)
            .AsInsert(RecordExtensions.FormObject(
                auditLog,
                blackList: [nameof(AuditLog.Id), nameof(AuditLog.CreatedAt)]
            ));

    public static Query List(Filter[] filters, Sort[] sorts, int?offset = null, int? limit = null)
    {
        var q = new Query(AuditLogConstants.TableName);
        q= q.Select(ListAttributes);
        if (offset > 0) q.Offset(offset.Value);
        q.Limit(limit ?? AuditLogConstants.DefaultPageSize);
        return q;
    }

    public static Query Count(Filter[] filters)
    {
        var q = new Query(AuditLogConstants.TableName);
        return q;
    }
    
}