using FormCMS.Activities.Models;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Activities.Services;


public interface IActivityCollectService
{
    Task Flush(DateTime? lastFlushTime, CancellationToken ct);
    Task EnsureActivityTables();
    Task<long> Toggle(string entityName, long recordId, string activityType, bool isActive, CancellationToken ct);
    Task<Dictionary<string,long>> Record(string cookieUserId,string entityName, long recordId, string[] activityType, CancellationToken ct);
    Task Visit(string cookieUserId, string url, CancellationToken ct);
    Task<Dictionary<string, StatusDto>> Get(string cookieUserId,string entityName, long recordId, CancellationToken ct);
    Task<Dictionary<string, long>> GetCountDict(string entityName, long recordId, string[] types, CancellationToken ct);
}