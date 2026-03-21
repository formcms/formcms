using System.Text.Json;
using FormCMS.Cms.Models;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Video.Models;

namespace FormCMS.Video.Workers;

public sealed class FFMpegWorker(
    ILogger<FFMpegWorker> logger,
    IStringMessageConsumer consumer,
    IFileStore fileStore,
    IStringMessageProducer producer
) : BackgroundService
{
    private IEnumerable<IConversionStrategy> strategies =
    [
        new HlsConversionStrategy(logger, fileStore, producer),
        new Mp3ConversionStrategy(logger, fileStore, producer),
        new M4aConversionStrategy(logger, fileStore, producer)
    ];
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("FFmpeg Worker starting at: {time}", DateTimeOffset.Now);

        await consumer.Subscribe(
            AssetTopics.ConvertVideo,
            "FFMpegWorker",
            async s =>
            {
                try
                {
                    var msg = ParseMessage(s);
                    if (msg is null) return;

                    if (msg.IsDelete)
                    {
                        var task = HlsConvertingTaskHelper.CreatTask(msg.Path);
                        await fileStore.DelByPrefix(task.StorageFolder, ct);
                    }
                    else
                    {
                        var strategy = strategies.FirstOrDefault(st => st.CanHandle(msg));
                        if (strategy != null)
                        {
                            await strategy.ExecuteAsync(msg, ct);
                        }
                        else
                        {
                            logger.LogWarning("No conversion strategy found for target format: {TargetFormat}", msg.TargetFormat);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing message: {RawMessage}", s);
                }
            },
            ct
        );
    }

    private ConvertVideoMessage? ParseMessage(string s)
    {
        var message = JsonSerializer.Deserialize<ConvertVideoMessage>(s);
        if (message is null)
        {
            logger.LogWarning("Could not deserialize message: {RawMessage}", s);
            return null;
        }

        if (string.IsNullOrEmpty(message.Path) || string.IsNullOrEmpty(message.TargetFormat))
        {
            logger.LogWarning(
                "Invalid message: Missing Path or TargetFormat. Raw: {RawMessage}",
                s
            );
            return null;
        }
        return message;
    }
}