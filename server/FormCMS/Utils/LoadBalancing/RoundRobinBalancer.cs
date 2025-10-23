namespace FormCMS.Utils.LoadBalancing;

public class RoundRobinBalancer<T>(T primary, T[]? replicas) where T : class
{
    private int _counter;

    public T Next
        => replicas is { Length: > 0 } ? replicas[GetNextIndex()] : primary;

    private int GetNextIndex()
    {
        var current = Interlocked.Increment(ref _counter);
        return Math.Abs(current % replicas!.Length);
    }
}