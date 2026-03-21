using System.Text.Json;
using FormCMS.Cms.Models;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Video.Models;
using Xabe.FFmpeg;

namespace FormCMS.Video.Workers;

public class M4aConversionStrategy(
    ILogger logger,
    IFileStore fileStore,
    IStringMessageProducer producer
) : IConversionStrategy
{
    public bool CanHandle(ConvertVideoMessage message) => message.TargetFormat == "m4a";

    public async Task ExecuteAsync(ConvertVideoMessage message, CancellationToken ct)
    {
        var task = HlsConvertingTaskHelper.CreatTask(message.Path);
        var tempPath = task.TempPath;
        var tempTargetPath = Path.ChangeExtension(tempPath, ".m4a");

        await fileStore.Download(task.StoragePath, tempPath, ct);
        
        var ffmpegPath = Environment.GetEnvironmentVariable("FFMPEG_EXEC_PATH");
        FFmpeg.SetExecutablesPath(ffmpegPath, ffmpegExeutableName: "ffmpeg");
        
        var mediaInfo = await FFmpeg.GetMediaInfo(tempPath);
        var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
        if (audioStream == null)
        {
             logger.LogWarning("No audio stream found in {Path}", message.Path);
             File.Delete(tempPath);
             return;
        }

        logger.LogInformation("Starting M4A conversion for {Path}", message.Path);

        // Compress to a smaller m4a size by setting audio bitrate (e.g. 64k for spoken word/audiobooks)
        var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(tempPath, tempTargetPath);
        conversion.AddParameter("-b:a 64k");

        // Throttle progress updates
        var lastPercent = 0;
        conversion.OnProgress += async (sender, args) =>
        {
            var percent = (int)(Math.Round(args.Duration.TotalSeconds / mediaInfo.Duration.TotalSeconds, 2) * 100);
            if (percent > lastPercent && percent < 100)
            {
                lastPercent = percent;
                var msg = new AssetUpdateMessage(
                    "m4a",
                    OriginalPath: message.Path,
                    NewUrl: null,
                    NewPath: null,
                    NewName: null,
                    NewType: null,
                    NewSize: null,
                    Progress: percent,
                    IsNewAsset: false,
                    UserId: message.UserId
                );
                // Fire and forget so we don't block the FFmpeg event thread
                _ = producer.Produce(AssetTopics.AssetUpdate, JsonSerializer.Serialize(msg));
            }
        };

        await conversion.Start(ct);
        
        var newPath = message.TargetPath ?? Path.ChangeExtension(task.StoragePath, ".m4a");

        await using (var stream = new FileStream(tempTargetPath, FileMode.Open, FileAccess.Read))
        {
            await fileStore.Upload(stream, newPath, ct);
        }
        
        File.Delete(tempTargetPath);
        
        await UpdateStatusForM4a(message.Path, newPath, message.UserId);
        logger.LogInformation("M4A conversion finished for [{OriginalPath}] to [{NewPath}]", message.Path, newPath);

        File.Delete(tempPath);
    }

    private async Task UpdateStatusForM4a(string originalPath, string newPath, string? userId)
    {
        var metadata = await fileStore.GetMetadata(newPath, CancellationToken.None);
        if (metadata == null)
        {
            logger.LogError("Could not get metadata for converted M4A file: {NewPath}", newPath);
            return;
        }

        var msg = new AssetUpdateMessage(
            "m4a",
            OriginalPath: originalPath,
            NewUrl: fileStore.GetUrl(newPath),
            NewPath: newPath,
            NewName: Path.GetFileName(newPath),
            NewType: metadata.ContentType,
            NewSize: metadata.Size,
            Progress: 100,
            IsNewAsset: true,
            UserId: userId
        );
        await producer.Produce(AssetTopics.AssetUpdate, JsonSerializer.Serialize(msg));
    }
}
