using FormCMS.Core.Descriptors;

namespace FormCMS.Activities.Services;

public static class ActivityQueryPluginConstants
{
    public const string TopList = "topList";
    public const string EntityName = "entity";
}
public interface IActivityQueryPlugin
{
    Task LoadCounts(LoadedEntity entity, IEnumerable<ExtendedGraphAttribute> attributes, IEnumerable<Record> records, CancellationToken ct);
    Task<Record[]> GetTopList(string entityName, int offset, int limit, CancellationToken ct);
}