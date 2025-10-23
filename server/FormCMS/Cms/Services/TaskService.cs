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
    IIdentityService identityService,
    
    IFileStore store,
    ShardGroup shardGroup,
    
    HttpClient httpClient
) : ITaskService
{ 
    public XEntity GetEntity()
    {
        EnsureHasPermission();
        return TaskHelper.Entity;
    }


    public async Task DeleteTaskFile(long id,CancellationToken ct)
    {
        EnsureHasPermission();
        var record =await shardGroup.PrimaryDao.Single(TaskHelper.ById(id),ct)?? throw new ResultException("Task not found");
        var task = record.ToObject<SystemTask>().Ok();
        await store.Del(task.GetPaths().Zip,ct);
        
        var query = TaskHelper.UpdateTaskStatus(new SystemTask(Id: id, TaskStatus: TaskStatus.Archived));
        await shardGroup.PrimaryDao.Exec(query,false,ct);
    }

    public async Task<string> GetTaskFileUrl(long id, CancellationToken ct)
    {
        EnsureHasPermission();
        var record =await shardGroup.PrimaryDao.Single(TaskHelper.ById(id),ct)?? throw new ResultException("Task not found");
        var task = record.ToObject<SystemTask>().Ok();
        return store.GetUrl(task.GetPaths().Zip);
    }

    public Task<long> AddEmitMessageTask(EmitMessageSetting setting)
    {
        EnsureHasPermission();
        var task = TaskHelper.InitTask(TaskType.EmitMessage, identityService.GetUserAccess()?.Name ?? "");
        task = task with { TaskSettings = setting.ToJson() };
        var query = TaskHelper.AddTask(task);
        return shardGroup.PrimaryDao.Exec(query,true);
    }
   
    public async Task<long> AddImportTask(IFormFile file)
    {
        EnsureHasPermission();
        var task = TaskHelper.InitTask(TaskType.Import, identityService.GetUserAccess()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        var id = await shardGroup.PrimaryDao.Exec(query,true);

        await using var stream = new FileStream(task.GetPaths().FullZip, FileMode.Create);
        await file.CopyToAsync(stream);
        return id;
    }
    
    public async Task<long> ImportDemoData()
    {
        EnsureHasPermission();
        
        var assembly = Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split("+").First()??"";
        var parts = version.Split('.');
        if (parts.Length > 3)
        {
            parts = parts[..3];
        }
        version = string.Join(".", parts);
        
        var url = $"https://github.com/FormCMS/FormCMS/raw/refs/heads/doc/etc/{title}-demo-data-{version}.zip";
        var task = TaskHelper.InitTask(TaskType.Import, identityService.GetUserAccess()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        var id = await shardGroup.PrimaryDao.Exec(query,true);

        await using var stream = new FileStream(task.GetPaths().FullZip, FileMode.Create);
        var fileBytes = await httpClient.GetByteArrayAsync(url);
        stream.Write(fileBytes, 0, fileBytes.Length);
        return id;
    }

    public Task<long> AddExportTask()
    {
        EnsureHasPermission();
        var task = TaskHelper.InitTask(TaskType.Export, identityService.GetUserAccess()?.Name ?? "");
        var query = TaskHelper.AddTask(task);
        return shardGroup.PrimaryDao.Exec(query,true);
    }

    public async Task<ListResponse> List(StrArgs args,int? offset, int? limit, CancellationToken ct)
    {
        EnsureHasPermission();
        var (filters, sorts) = QueryStringParser.Parse(args);
        var query = TaskHelper.List(offset, limit);
        var items = await shardGroup.PrimaryDao.Many(query, TaskHelper.Columns,filters,sorts,ct);
        var count = await shardGroup.PrimaryDao.Count(TaskHelper.Query(),TaskHelper.Columns,filters,ct);
        return new ListResponse(items,count);
    }

    private void EnsureHasPermission()
    {
        if (identityService.GetUserAccess()?.CanAccessAdmin != true)
            throw new ResultException("You don't have permission ");
    }
}