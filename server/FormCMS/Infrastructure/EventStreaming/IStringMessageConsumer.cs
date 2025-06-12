namespace FormCMS.Infrastructure.EventStreaming;

public interface IStringMessageConsumer
{
    Task Subscribe(string topic, Func<string, Task> handler, CancellationToken ct);
}
