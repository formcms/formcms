namespace FormCMS.Infrastructure.EventStreaming;

public interface IStringMessageConsumer
{
    Task Subscribe(Func<string, Task> handler, CancellationToken ct);
    Task SubscribeTopic(string topic, Func<string, Task> handler, CancellationToken ct);
}
