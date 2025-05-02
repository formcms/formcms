using FormCMS.Utils.DisplayModels;

namespace FormCMS.Activities.Services;

public record ActivitySettings(
    bool EnableBuffering,
    HashSet<string> ToggleActivities,
    HashSet<string> RecordActivities,
    HashSet<string> AutoRecordActivities
);

public static class ActivityServiceExtensions
{
    public static string[] AllCountTypes(this ActivitySettings activitySettings)
        => activitySettings.AutoRecordActivities
            .Concat(activitySettings.ToggleActivities)
            .Concat(activitySettings.RecordActivities)
            .ToArray();
}


public interface IActivityService
{
    Task LoadCounts(string entityName, string primaryKey, HashSet<string>fields, IEnumerable<Record> records, CancellationToken ct);
    Task<Record[]> GetTopVisitCount(int topN, CancellationToken ct);
    Task<Record[]> GetDailyPageVisitCount(int daysAgo, bool authed, CancellationToken ct);
    Task<Record[]> GetDailyActivityCount(int daysAgo,CancellationToken ct);
    Task<ListResponse> List(string activityType, StrArgs args, int? offset, int? limit, CancellationToken ct);
    Task Flush(DateTime? lastFlushTime, CancellationToken ct);
    Task EnsureActivityTables();
    Task<long> Toggle(string entityName, long recordId, string activityType, bool isActive, CancellationToken ct);
    Task<Dictionary<string,long>> Record(string cookieUserId,string entityName, long recordId, string[] activityType, CancellationToken ct);
    Task Visit(string cookieUserId, string url, CancellationToken ct);
    Task<Dictionary<string, StatusDto>> Get(string cookieUserId,string entityName, long recordId, CancellationToken ct);
    Task Delete(long id, CancellationToken ct = default);
}