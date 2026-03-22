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
    public bool CanHandle(ConvertVideoMessage message) => message.TargetPath?.EndsWith($".{ConvertVideoFormats.M4a}") == true;

    public async Task ExecuteAsync(ConvertVideoMessage message, CancellationToken ct)
    {
        var task = HlsConvertingTaskHelper.CreatTask(message.Path);
        var tempPath = task.TempPath;
        var tempTargetPath = Path.ChangeExtension(tempPath, $".{ConvertVideoFormats.M4a}");
        var targetPath = message.TargetPath ?? throw new InvalidOperationException("TargetPath is required");

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
                    NewPath: targetPath, // use targetPath for progress updates
                    Progress: percent
                );
                // Fire and forget so we don't block the FFmpeg event thread
                _ = producer.Produce(AssetTopics.AssetUpdate, JsonSerializer.Serialize(msg));
            }
        };

        await conversion.Start(ct);

        await using (var stream = new FileStream(tempTargetPath, FileMode.Open, FileAccess.Read))
        {
            await fileStore.Upload(stream, targetPath, ct);
        }
        
        File.Delete(tempTargetPath);
        
        await UpdateStatusForM4a(targetPath);
        logger.LogInformation("M4A conversion finished for [{OriginalPath}] to [{NewPath}]", message.Path, targetPath);

        File.Delete(tempPath);
    }

    private async Task UpdateStatusForM4a(string targetPath)
    {
        var msg = new AssetUpdateMessage(
            NewPath: targetPath,
            Progress: 100
        );
        await producer.Produce(AssetTopics.AssetUpdate, JsonSerializer.Serialize(msg));
    }
}