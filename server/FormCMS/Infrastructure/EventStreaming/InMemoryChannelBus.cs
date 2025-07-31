using System.Collections.Concurrent;
using System.Threading.Channels;

namespace FormCMS.Infrastructure.EventStreaming;

public class InMemoryChannelBus : IStringMessageProducer, IStringMessageConsumer
{
    // Structure: topic -> group -> channel
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Channel<string>>> _topicGroups = new();

    private Channel<string> GetOrCreateChannel(string topic, string group)
    {
        var groupDict = _topicGroups.GetOrAdd(topic, _ => new ConcurrentDictionary<string, Channel<string>>());

        return groupDict.GetOrAdd(group, _ =>
            Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));
    }

    public Task Produce(string topic, string msg)
    {
        if (_topicGroups.TryGetValue(topic, out var groupDict))
        {
            var tasks = new List<Task>();

            foreach (var kvp in groupDict)
            {
                var writer = kvp.Value.Writer;
                tasks.Add(writer.WriteAsync(msg).AsTask());
            }

            return Task.WhenAll(tasks); // Wait for all writes to complete
        }

        return Task.CompletedTask;
    }

    public Task Subscribe(string topic, string group, Func<string, Task> handler, CancellationToken ct)
    {
        var channel = GetOrCreateChannel(topic, group);

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