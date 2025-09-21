namespace FormCMS.Infrastructure.RelationDbDao;

public record ShardConfig(string leaderConnStr, string[] followConnStrings, int Start, int End );
public class Shard(IRelationDbDao leader, IRelationDbDao[]? followers = null, int start = 0, int end = 12)
{
    private int _index ;
    public readonly int End = end;

    public IRelationDbDao Leader { get; } = leader;
    public bool InRange(int idx) => idx >= start && idx < end;

    public IRelationDbDao Follower
    {
        get
        {
            if (followers is null || followers.Length == 0) return Leader;

            var i = Interlocked.Increment(ref _index);
            var idx = Math.Abs(i % followers.Length);
            return followers[idx];
        }
    }
}