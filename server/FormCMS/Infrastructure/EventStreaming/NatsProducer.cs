using NATS.Client.Core;

namespace FormCMS.Infrastructure.EventStreaming;

public class NatsProducer(ILogger<NatsProducer> logger,INatsConnection connection):IStringMessageProducer
{
    public async Task Produce(string topic, string msg)
    {
        await connection.PublishAsync(topic, msg);
        logger.LogInformation("Produced to topic {topic}, message {msg}", topic, msg);
    }
}