namespace FormCMS.Engagements.Services;


public interface IEngagementCollectService
{
    Task<long> ToggleEngagement(string entityName, string recordId, string activityType, bool isActive, CancellationToken ct);
    Task<Dictionary<string,long>> MarkEngaged(string useId, string entityName, string recordId, string[] activityTypes, CancellationToken ct);
    Task<Dictionary<string,long>> MarkEngagedForCurrentUser(string cookieUserId,string entityName, string recordId, string[] activityType, CancellationToken ct);
    Task RecordPageVisit(string cookieUserId, string url, CancellationToken ct);
    Task<Dictionary<string, EngagementCountDto>> AutoEngageAndGetCounts(string cookieUserId,string entityName, string recordId, CancellationToken ct);
    Task<Dictionary<string, long>> GetEngagementCounts(string entityName, string recordId, string[] types, CancellationToken ct);
    Task<string[]> GetEngagedRecordIds(string entityName, string activityType, string[] recordIds, CancellationToken ct);
    Task FlushBuffers(DateTime? lastFlushTime, CancellationToken ct);
}