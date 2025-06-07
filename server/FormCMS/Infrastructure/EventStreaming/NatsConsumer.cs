using NATS.Client.Core;

namespace FormCMS.Infrastructure.EventStreaming;

public class NatsConsumer(ILogger<NatsConsumer> logger, INatsConnection connection)
    : IStringMessageConsumer
{
    public async Task Subscribe(Func<string, Task> handler, CancellationToken ct)
    {
        await foreach (
            var msg in connection.SubscribeAsync<string>(
                subject: Topics.CmsCrud,
                cancellationToken: ct
            )
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

    public async Task SubscribeTopic(string topic, Func<string, Task> handler, CancellationToken ct)
    {
        await foreach (
            var msg in connection.SubscribeAsync<string>(subject: topic, cancellationToken: ct)
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
}
