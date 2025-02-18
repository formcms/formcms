using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface ITaskService
{
    Task<int> AddExportTask();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct);
    
    string GetExportedFileDownloadUrl(int id);
    
    Task DeleteExportedFile(int id);
    XEntity GetEntity();
    Task EnsureTable();
}