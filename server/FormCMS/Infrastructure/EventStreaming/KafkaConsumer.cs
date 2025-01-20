using Confluent.Kafka;

namespace FormCMS.Infrastructure.EventStreaming;

public class KafkaConsumer(ILogger<KafkaConsumer> logger, IConsumer<string, string> consumer) : IStringMessageConsumer
{
    public async Task Subscribe(Func<string, Task> handler, CancellationToken ct)
    {
        consumer.Subscribe(Topics.CmsCrud);
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