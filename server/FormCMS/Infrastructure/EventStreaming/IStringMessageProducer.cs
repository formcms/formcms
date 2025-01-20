namespace FormCMS.Infrastructure.EventStreaming;

public interface IStringMessageProducer
{
    Task Produce(string topic, string msg);
}
