using System.Reflection;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.FileStore;
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
    public async Task DeleteTaskFile(long id,CancellationToken ct)
    {
        EnsureHasPermission();
        var record =await executor.Single(TaskHelper.ById(id),ct)?? throw new ResultException("Task not found");
        var task = record.ToObject<SystemTask>().Ok();
        await store.Del(task.GetPaths().Zip,ct);
        
        var query = TaskHelper.UpdateTaskStatus(new SystemTask(Id: id, TaskStatus: TaskStatus.Archived));
        await executor.Exec(query,false,ct);
    }
    
    public async Task<string> GetTaskFileUrl(long id, CancellationToken ct)
    {
        EnsureHasPermission();
        var record =await executor.Single(TaskHelper.ById(id),ct)?? throw new ResultException("Task not found");
        var task = record.ToObject<SystemTask>().Ok();
        return store.GetUrl(task.GetPaths().Zip);
    }

    public XEntity GetEntity()
    {
        EnsureHasPermission();
        return TaskHelper.Entity;
    }

    public Task EnsureTable()
        =>migrator.MigrateTable(TaskHelper.TableName,TaskHelper.Columns);

    public async Task<long> AddImportTask(IFormFile file)
    {
        EnsureHasPermission();
        var task = TaskHelper.InitTask(TaskType.Import, profileService.GetInfo()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        var id = await executor.Exec(query,true);

        await using var stream = new FileStream(task.GetPaths().FullZip, FileMode.Create);
        await file.CopyToAsync(stream);
        return id;
    }
    
    public async Task<long> ImportDemoData()
    {
        EnsureHasPermission();
        
        var assembly = Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split("+").First();
        var url = $"https://github.com/FormCMS/FormCMS/raw/refs/heads/doc/etc/{title}-demo-data-{version}.zip";
        var task = TaskHelper.InitTask(TaskType.Import, profileService.GetInfo()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        var id = await executor.Exec(query,true);

        await using var stream = new FileStream(task.GetPaths().FullZip, FileMode.Create);
        byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
        stream.Write(fileBytes, 0, fileBytes.Length);
        return id;
    }

    public Task<long> AddExportTask()
    {
        EnsureHasPermission();
        var task = TaskHelper.InitTask(TaskType.Export, profileService.GetInfo()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        return executor.Exec(query,true);
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
        if (profileService.GetInfo()?.CanAccessAdmin != true)
            throw new ResultException("You don't have permission ");
    }
}