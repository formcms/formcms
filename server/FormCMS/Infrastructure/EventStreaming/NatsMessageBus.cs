using NATS.Client.Core;

namespace FormCMS.Infrastructure.EventStreaming;

public class NatsMessageBus(
    ILogger<NatsMessageBus> logger,
    INatsConnection connection
)
    : IStringMessageConsumer, IStringMessageProducer
{

    public async Task Subscribe(string topic, string group, Func<string, Task> handler, CancellationToken ct)
    {
        await foreach (
            var msg in connection.SubscribeAsync<string>(subject: topic, queueGroup: group, cancellationToken: ct)
        )
        {
            if (msg.Data is not null)
            {
                await handler(msg.Data);
            }
            else
            {
                logger.LogError("Received unexpected message");
            }
        }
    }

    public async Task Produce(string topic, string msg)
    {
        await connection.PublishAsync(topic, msg);
        logger.LogInformation("Produced to topic {topic}, message {msg}", topic, msg);
    }
}