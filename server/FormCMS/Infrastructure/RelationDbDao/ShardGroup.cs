using FormCMS.Utils.LoadBalancing;

namespace FormCMS.Infrastructure.RelationDbDao;

public record ShardConfig(DatabaseProvider DatabaseProvider,  string LeadConnStr, string[]? FollowConnStrings = null, int Start = 0, int End = 12);

public class ShardGroup(IRelationDbDao primaryDao, IRelationDbDao[]? replicas = null, int start = 0, int end = 12):IDisposable
{
    public readonly int End = end;
    public bool InRange(int idx) => idx >= start && idx < End;

    private readonly RoundRobinBalancer<IRelationDbDao> _selector = new (primaryDao,replicas);
    private readonly IRelationDbDao _primaryDao = primaryDao;
    public IRelationDbDao PrimaryDao { get; } = primaryDao;
    public IRelationDbDao ReplicaDao => _selector.Next;
    public void Dispose()
    {
        _primaryDao.Dispose();
        if (replicas != null)
        {
            foreach (var dao in replicas)
            {
                dao.Dispose();
                
            }
        }
    }
}