using FormCMS.Core.Tasks;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface ITaskService
{
    XEntity GetEntity();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct);
    
    Task<long> AddImportTask(IFormFile file);
    Task<long> ImportDemoData();
    
    Task<long> AddExportTask();
    
    Task<long> AddEmitMessageTask(EmitMessageSetting setting);
    
    Task<string> GetTaskFileUrl(long id, CancellationToken ct);
    Task DeleteTaskFile(long id, CancellationToken ct);
}