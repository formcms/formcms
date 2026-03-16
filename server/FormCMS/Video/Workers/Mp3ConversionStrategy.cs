using System.Text.Json;
using FormCMS.Cms.Models;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Video.Models;
using Xabe.FFmpeg;

namespace FormCMS.Video.Workers;

public class Mp3ConversionStrategy(
    ILogger logger,
    IFileStore fileStore,
    IStringMessageProducer producer
) : IConversionStrategy
{
    public bool CanHandle(ConvertVideoMessage message) => message.TargetFormat == "mp3";

    public async Task ExecuteAsync(ConvertVideoMessage message, CancellationToken ct)
    {
        var task = HlsConvertingTaskHelper.CreatTask(message.Path);
        var tempPath = task.TempPath;
        var tempTargetPath = Path.ChangeExtension(tempPath, ".mp3");

        await fileStore.Download(task.StoragePath, tempPath, ct);
        
        var ffmpegPath = Environment.GetEnvironmentVariable("FFMPEG_EXEC_PATH");
        FFmpeg.SetExecutablesPath(ffmpegPath, ffmpegExeutableName: "ffmpeg");
        
        var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(tempPath, tempTargetPath);
        await conversion.Start(ct);
        
        var newPath = message.TargetPath ?? Path.ChangeExtension(task.StoragePath, ".mp3");
        await using (var stream = new FileStream(tempTargetPath, FileMode.Open, FileAccess.Read))
        {
            await fileStore.Upload(stream, newPath, ct);
        }
        
        File.Delete(tempTargetPath);
        File.Delete(tempPath);
        
        await UpdateStatusForMp3(message.Path, newPath, message.UserId);
        logger.LogInformation("MP3 conversion finished for [{OriginalPath}] to [{NewPath}]", message.Path, newPath);
    }

    private async Task UpdateStatusForMp3(string originalPath, string newPath, string? userId)
    {
        var metadata = await fileStore.GetMetadata(newPath, CancellationToken.None);
        if (metadata == null)
        {
            logger.LogError("Could not get metadata for converted MP3 file: {NewPath}", newPath);
            return;
        }

        var msg = new AssetUpdateMessage(
            "mp3",
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
