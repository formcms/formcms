using FormCMS.Core.Descriptors;

namespace FormCMS.Engagements.Services;

public static class EngagementQueryPluginConstants
{
    public const string TopList = "topList";
    public const string EntityName = "entity";
}
public interface IEngagementQueryPlugin
{
    Task LoadCounts(LoadedEntity entity, GraphNode[] nodes, Record[] records, CancellationToken ct);
    Task<Record[]> GetTopList(string entityName, int offset, int limit, CancellationToken ct);
}