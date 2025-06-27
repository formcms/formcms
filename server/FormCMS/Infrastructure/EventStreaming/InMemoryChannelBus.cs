using System.Collections.Concurrent;
using System.Threading.Channels;

namespace FormCMS.Infrastructure.EventStreaming;

public class InMemoryChannelBus : IStringMessageProducer, IStringMessageConsumer
{
    private readonly ConcurrentDictionary<string, Channel<string>> _channels = new();

    private Channel<string> GetOrCreateChannel(string topic)
    {
        return _channels.GetOrAdd(topic, _ =>
            Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));
    }

    public Task Produce(string topic, string msg)
    {
        var channel = GetOrCreateChannel(topic);
        return channel.Writer.WriteAsync(msg).AsTask();
    }

    //In memory channel won't work for distributed deployment, just ignore group
    public Task Subscribe(string topic, string group, Func<string, Task> handler, CancellationToken ct)
    {
        var channel = GetOrCreateChannel(topic);

        // Run handler in background
        return Task.Run(async () =>
        {
            var reader = channel.Reader;
            while (await reader.WaitToReadAsync(ct))
            {
                while (reader.TryRead(out var msg))
                {
                    await handler(msg);
                }
            }
        }, ct);
    }
}