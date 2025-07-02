namespace FormCMS.Infrastructure.EventStreaming;

public interface IStringMessageConsumer
{
    //only one consumer in the consumer group can get message 
    Task Subscribe(string topic, string group, Func<string, Task> handler, CancellationToken ct);
}
