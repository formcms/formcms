using FormCMS.Utils.DisplayModels;

namespace FormCMS.Activities.Services;

public record ActivitySettings(
    bool EnableBuffering,
    HashSet<string> ToggleActivities,
    HashSet<string> RecordActivities,
    HashSet<string> AutoRecordActivities
);

public record StatusDto(bool Active, long Count);

public interface IActivityService
{
    Task<ListResponse> List(string activityType, StrArgs args, int? offset, int? limit, CancellationToken ct);
    Task Flush(DateTime? lastFlushTime, CancellationToken ct);
    Task EnsureActivityTables();
    Task<long> Toggle(string entityName, long recordId, string activityType, bool isActive, CancellationToken ct);
    Task<Dictionary<string,long>> Record(string entityName, long recordId, string[] activityType, CancellationToken ct);
    Task<Dictionary<string, StatusDto>> Get(string entityName, long recordId, CancellationToken ct);
    Task Delete(long id, CancellationToken ct = default);
}