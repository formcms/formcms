using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;
using Task = System.Threading.Tasks.Task;
using TaskStatus = FormCMS.Core.Tasks.TaskStatus;

namespace FormCMS.Cms.Services;

public class TaskService(
    IProfileService profileService,
    KateQueryExecutor executor,
    DatabaseMigrator migrator,
    IFileStore store
) : ITaskService
{
    public Task DeleteExportedFile(int id)
    {
        store.Del(TaskHelper.GetExportFileName(id));
        var query = TaskHelper.UpdateTaskStatus(new SystemTask(Id: id, TaskStatus: TaskStatus.Archived));
        return executor.ExecAndGetAffected(query);
    }
    
    public string GetExportedFileDownloadUrl(int id)
    {
        return store.GetDownloadPath(TaskHelper.GetExportFileName(id));
    }

    public XEntity GetEntity() => TaskHelper.Entity;
    public Task EnsureTable()
        =>migrator.MigrateTable(TaskHelper.TableName,TaskHelper.Columns);

    public Task<int> AddExportTask()
    {
        var query = TaskHelper.AddExportTask(profileService.GetInfo()?.Name ?? "");
        return executor.ExeAndGetId(query);
    }

    public async Task<ListResponse> List(StrArgs args,int? offset, int? limit, CancellationToken ct)
    {
        EnsureHasPermission();
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = TaskHelper.List(offset, limit);
        var items = await executor.Many(query, TaskHelper.Columns,filters,sorts,ct);
        var count = await executor.Count(TaskHelper.Query(),TaskHelper.Columns,filters,ct);
        return new ListResponse(items,count);
    }


    private void EnsureHasPermission()
    {
        var menus = profileService.GetInfo()?.AllowedMenus??[];
        if (!menus.Contains(Menus.MenuTasks))
            throw new ResultException("You don't have permission ");
    }
}