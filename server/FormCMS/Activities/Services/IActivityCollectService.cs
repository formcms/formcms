namespace FormCMS.Activities.Services;


public interface IActivityCollectService
{
    Task Flush(DateTime? lastFlushTime, CancellationToken ct);
    Task<long> Toggle(string entityName, string recordId, string activityType, bool isActive, CancellationToken ct);
    Task RecordForMessageBroker(string useId, string entityName, string recordId, string[] activityTypes, CancellationToken ct);
    Task<Dictionary<string,long>> RecordForWebRequest(string cookieUserId,string entityName, string recordId, string[] activityType, CancellationToken ct);
    Task Visit(string cookieUserId, string url, CancellationToken ct);
    Task<Dictionary<string, ActiveCount>> GetSetActiveCount(string cookieUserId,string entityName, string recordId, CancellationToken ct);
    Task<Dictionary<string, long>> GetCountDict(string entityName, string recordId, string[] types, CancellationToken ct);
    Task<string[]> GetCurrentUserActiveStatus(string entityName, string activityType, string[] recordIds, CancellationToken ct);
}