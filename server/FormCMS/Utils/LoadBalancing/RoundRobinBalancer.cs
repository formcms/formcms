namespace FormCMS.Utils.LoadBalancing;

public class RoundRobinBalancer<TPrimary, TReplica>(TPrimary primary, TReplica[]? replicas)
    where TReplica : class
    where TPrimary : TReplica
{
    private int _counter;

    public TReplica Next
        => replicas is { Length: > 0 } ? replicas[GetNextIndex()] : primary;

    private int GetNextIndex()
    {
        var current = Interlocked.Increment(ref _counter);
        return Math.Abs(current % replicas!.Length);
    }
}