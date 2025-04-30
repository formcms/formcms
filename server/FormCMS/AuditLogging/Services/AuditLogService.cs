using FormCMS.AuditLogging.Models;
using FormCMS.Cms.Services;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.AuditLogging.Services;

public class AuditLogService(
    IProfileService profileService,
    KateQueryExecutor executor,
    DatabaseMigrator migrator,
    IRelationDbDao dao
    ):IAuditLogService
{
    public  Task<Record[]> GetActionCounts(int daysAgo,CancellationToken ct )
    {
        EnsureHasPermission();
        return executor.Many(AuditLogHelper.GetDailyActionCount(dao.CastDate,daysAgo), ct);
    }
    public async Task<AuditLog> Single(long id, CancellationToken ct = default)
    {
        EnsureHasPermission();
        var query = AuditLogHelper.ById(id);
        var item = await executor.Single(query, ct)?? throw new ResultException("No record found");;
        return item.ToObject<AuditLog>().Ok();
    }
    
    public async Task<ListResponse> List(StrArgs args,int? offset, int? limit, CancellationToken ct)
    {
        EnsureHasPermission();
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = AuditLogHelper.List(offset, limit);
        var items = await executor.Many(query, AuditLogHelper.Columns,filters,sorts,ct);
        var count = await executor.Count(AuditLogHelper.Count(),AuditLogHelper.Columns,filters,ct);
        return new ListResponse(items,count);
    }

    public Task AddLog(ActionType actionType, string entityName, string id, string label, Record record)
    {
        var currentUser = profileService.GetInfo();
        var log = new AuditLog(
            Id: 0,
            UserId: currentUser?.Id??"",
            UserName: currentUser?.Email??"",
            Action: actionType,
            EntityName: entityName,
            RecordId: id,
            RecordLabel: label,
            Payload: record,
            CreatedAt: DateTime.Now
        );
        return executor.Exec(log.Insert(),true);
    }

    public  Task EnsureAuditLogTable()
        =>migrator.MigrateTable(AuditLogConstants.TableName,AuditLogHelper.Columns);

    public XEntity GetAuditLogEntity() => AuditLogHelper.Entity;

    private void EnsureHasPermission()
    {
        var menus = profileService.GetInfo()?.AllowedMenus??[];
        if (!menus.Contains(AuditLoggingConstants.MenuId))
            throw new ResultException("You don't have permission to view audit logs");
    }
}