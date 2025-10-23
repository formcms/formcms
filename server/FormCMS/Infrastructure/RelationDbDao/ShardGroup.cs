using FormCMS.Utils.LoadBalancing;

namespace FormCMS.Infrastructure.RelationDbDao;

public record ShardConfig(string LeadConnStr, string[]? FollowConnStrings = null, int Start = 0, int End = 12);

public class ShardGroup(IRelationDbDao primaryDao, IRelationDbDao[]? replicas = null, int start = 0, int end = 12)
{
    public readonly int End = end;
    public bool InRange(int idx) => idx >= start && idx < End;

    private readonly RoundRobinBalancer<IRelationDbDao> _selector = new (primaryDao,replicas);
    public IRelationDbDao PrimaryDao { get; } = primaryDao;
    public IRelationDbDao ReplicaDao => _selector.Next;
}