using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface ITaskService
{
    Task<int> AddImportTask(IFormFile file);
    Task<int> ImportDemoData();
    Task<int> AddExportTask();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct);
    
    Task<string> GetTaskFileUrl(int id, CancellationToken ct);
    Task DeleteTaskFile(int id, CancellationToken ct);
    XEntity GetEntity();
    Task EnsureTable();
    
}