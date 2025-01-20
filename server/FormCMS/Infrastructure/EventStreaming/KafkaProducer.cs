using Confluent.Kafka;

namespace FormCMS.Infrastructure.EventStreaming;

public sealed class KafkaProducer(ILogger<KafkaProducer> logger,IProducer<string,string> producer):IStringMessageProducer
{
    public async Task Produce(string topic, string message)
    {
        await producer.ProduceAsync(topic,
            new Message<string, string> { Value = message });
        logger.LogInformation("Produced Message: topic={topic}, message={message}", topic, message);
    }
}