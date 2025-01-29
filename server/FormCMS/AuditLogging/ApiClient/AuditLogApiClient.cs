using FluentResults;
using FormCMS.AuditLogging.Models;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.AuditLogging.ApiClient;

public class AuditLogApiClient(HttpClient client)
{
    public Task<Result<ListResponse>> List(string qs)
        => client.GetResult<ListResponse>("/api/audit_log?" + qs);

    public Task<Result<AuditLog>> Single(int id)
        => client.GetResult<AuditLog>($"/api/audit_log/{id}");
    
    public Task<Result<AuditLog>> AuditEntity()
        => client.GetResult<AuditLog>($"/api/audit_log/entity");
}
