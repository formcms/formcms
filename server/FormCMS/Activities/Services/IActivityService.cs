namespace FormCMS.Activities.Services;

public record ActivitySettings(
    HashSet<string> ToggleActivities,
    HashSet<string> RecordActivities
);

public record StatusDto(bool Active, long Count);
public interface IActivityService
{
    Task Flush(DateTime lastFlushTime, CancellationToken ct);
    Task EnsureActivityTables();
    Task<long> ToggleActive(string entityName, long recordId, string activityType, bool isActive, CancellationToken ct);
    Task<Dictionary<string, StatusDto>> Get(string entityName, long recordId, CancellationToken ct);
}