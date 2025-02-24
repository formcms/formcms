using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Task = System.Threading.Tasks.Task;
using TaskStatus = FormCMS.Core.Tasks.TaskStatus;

namespace FormCMS.Cms.Services;

public class TaskService(
    IProfileService profileService,
    KateQueryExecutor executor,
    DatabaseMigrator migrator,
    IFileStore store,
    HttpClient httpClient
) : ITaskService
{
    public async Task DeleteTaskFile(int id,CancellationToken ct)
    {
        var record =await executor.Single(TaskHelper.ById(id),ct)?? throw new ResultException("Task not found");
        var task = record.ToObject<SystemTask>().Ok();
        await store.Del(task.GetPaths().Zip);
        
        var query = TaskHelper.UpdateTaskStatus(new SystemTask(Id: id, TaskStatus: TaskStatus.Archived));
        await executor.ExecAndGetAffected(query,ct);
    }
    
    public async Task<string> GetTaskFileUrl(int id, CancellationToken ct)
    {
        var record =await executor.Single(TaskHelper.ById(id),ct)?? throw new ResultException("Task not found");
        var task = record.ToObject<SystemTask>().Ok();
        return store.GetDownloadPath(task.GetPaths().Zip);
    }

    public XEntity GetEntity() => TaskHelper.Entity;
    public Task EnsureTable()
        =>migrator.MigrateTable(TaskHelper.TableName,TaskHelper.Columns);

    public async Task<int> AddImportTask(IFormFile file)
    {
        var task = TaskHelper.InitTask(TaskType.Import, profileService.GetInfo()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        var id = await executor.ExeAndGetId(query);

        await using var stream = new FileStream(task.GetPaths().FullZip, FileMode.Create);
        await file.CopyToAsync(stream);
        return id;
    }
    
    public async Task<int> ImportDemoData()
    {
        const string url = "https://github.com/FormCMS/FormCMS/raw/refs/heads/doc/etc/demo-data.zip";
        var task = TaskHelper.InitTask(TaskType.Import, profileService.GetInfo()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        var id = await executor.ExeAndGetId(query);

        await using var stream = new FileStream(task.GetPaths().FullZip, FileMode.Create);
        byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
        stream.Write(fileBytes, 0, fileBytes.Length);
        return id;
    }

    public Task<int> AddExportTask()
    {
        var task = TaskHelper.InitTask(TaskType.Export, profileService.GetInfo()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
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