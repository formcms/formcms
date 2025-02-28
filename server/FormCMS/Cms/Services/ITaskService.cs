using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface ITaskService
{
    Task<long> AddImportTask(IFormFile file);
    Task<long> ImportDemoData();
    Task<long> AddExportTask();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct);
    
    Task<string> GetTaskFileUrl(long id, CancellationToken ct);
    Task DeleteTaskFile(long id, CancellationToken ct);
    XEntity GetEntity();
    Task EnsureTable();
    
}