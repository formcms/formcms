using Confluent.Kafka;

namespace FormCMS.Infrastructure.EventStreaming;

public class KafkaConsumer(ILogger<KafkaConsumer> logger, IConsumer<string, string> consumer) : IStringMessageConsumer
{
    public async Task Subscribe(string topic, Func<string, Task> handler, CancellationToken ct)
    {
        consumer.Subscribe(topic);
        while (!ct.IsCancellationRequested)
        {
            var s = consumer.Consume(ct).Message.Value;
            if (s is not null)
            {
                await handler(s);
            }
            else
            {
                logger.LogError("Got null message");
            }
        }
    }

}