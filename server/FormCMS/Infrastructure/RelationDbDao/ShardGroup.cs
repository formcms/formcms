using FormCMS.Utils.LoadBalancing;

namespace FormCMS.Infrastructure.RelationDbDao;

public record ShardConfig(DatabaseProvider DatabaseProvider,  string LeadConnStr, string[]? FollowConnStrings = null, int Start = 0, int End = 12);

public class ShardGroup(IPrimaryDao primaryDao, IReplicaDao[]? replicas = null, int start = 0, int end = 12):IDisposable
{
    public readonly int End = end;
    public bool InRange(int idx) => idx >= start && idx < End;

    private readonly RoundRobinBalancer<IPrimaryDao,IReplicaDao> _selector = new (primaryDao,replicas);
    private readonly IPrimaryDao _primaryDao = primaryDao;
    public IPrimaryDao PrimaryDao { get; } = primaryDao;
    public IReplicaDao ReplicaDao => _selector.Next;
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