using System.Text.Json;
using FormCMS.Cms.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.Infrastructure.EventStreaming;

namespace FormCMS.Cms.Workers;

public sealed class AssetUpdateMessageHandler(
    ILogger<AssetUpdateMessageHandler> logger,
    IStringMessageConsumer consumer,
    IServiceProvider provider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("AssetUpdateMessageHandler starting at: {time}", DateTimeOffset.Now);

        await consumer.Subscribe(
            AssetTopics.AssetUpdate,
            "AssetUpdateMessageHandler",
            async s =>
            {
                try
                {
                    var msg = JsonSerializer.Deserialize<AssetUpdateMessage>(s);
                    if (msg is null)
                    {
                        logger.LogWarning("Could not deserialize AssetUpdateMessage: {Raw}", s);
                        return;
                    }

                    await using var scope = provider.CreateAsyncScope();
                    var assetService = scope.ServiceProvider.GetRequiredService<IAssetService>();

                    var asset = new Asset(
                        Path: msg.NewPath,
                        Progress: msg.Progress,
                        Url: ""
                    );
                    
                    await assetService.UpdateConvertProgress(asset, ct);
                    logger.LogInformation("AssetUpdateMessageHandler processed: {Path}", msg.NewPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing AssetUpdate message");
                }
            },
            ct
        );
    }
}