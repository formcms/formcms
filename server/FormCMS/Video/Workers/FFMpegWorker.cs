using System.Text.Json;
using FormCMS.Cms.Models;
using FormCMS.Core.Assets;
using FormCMS.Core.Auth;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Models;
using Xabe.FFmpeg;

namespace FormCMS.Video.Workers;

public sealed class FFMpegWorker(
    ILogger<FFMpegWorker> logger,
    IStringMessageConsumer consumer,
    IStringMessageProducer producer,
    IFileStore fileStore
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("FFmpeg Worker starting at: {time}", DateTimeOffset.Now);

        // Subscribe to the message topic ONCE
        await consumer.Subscribe(
            VideoTopics.Rdy4FfMpeg,
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
                        await DoConvert(msg); 
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing message");
                }
            },
            ct
        );
    }

    private async Task DoConvert(FFMpegMessage msg)
    {
        var task = HlsConvertingTaskHelper.CreatTask(msg.Path);
        var tempPath = task.TempPath;
        var tempTargetPath = task.TempTargetPath;

        if (msg.TargetFormat == "mp3")
        {
            tempTargetPath = Path.ChangeExtension(tempPath, ".mp3");
        }

        await fileStore.Download(task.StoragePath, tempPath, CancellationToken.None);
        await RunConversion(tempPath, tempTargetPath, msg.TargetFormat);
        
        if (msg.TargetFormat == "mp3")
        {
            var newPath = msg.TargetPath ?? Path.ChangeExtension(task.StoragePath, ".mp3");
            await fileStore.Upload(File.OpenRead(tempTargetPath), newPath, CancellationToken.None);
            File.Delete(tempTargetPath);
            File.Delete(tempPath);
            await UpdateStatusForMp3(msg.Path, newPath);
        }
        else // m3u8 conversion
        {
            await fileStore.UploadFolder(task.TempFolder, task.StorageFolder,CancellationToken.None);
            File.Delete(tempPath);
            Directory.Delete(task.TempFolder, recursive: true);
            await UpdateStatus(task);
        }
        logger.LogInformation( "Processed message. Task={task}", msg.Path);       
    }

    private FFMpegMessage? ParseMessage(string s)
    {
        var message = JsonSerializer.Deserialize<FFMpegMessage>(s);
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
    
    private async Task RunConversion(string inputPath, string outputPath, string targetFormat)
    {
        var path = Environment.GetEnvironmentVariable("FFMPEG_EXEC_PATH");
        FFmpeg.SetExecutablesPath(path, ffmpegExeutableName: "ffmpeg");

        IConversion conversion;
        if (targetFormat == "mp3")
        {
            conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(inputPath, outputPath);
        }
        else // m3u8
        {
            conversion = await FFmpeg.Conversions.FromSnippet.Convert(inputPath, outputPath);
        }

        await conversion.Start();
        logger.LogInformation("Finished conversion file [{InputPath}] to [{OutputPath}]", inputPath, outputPath);
    }

    private async Task UpdateStatus(HlsConvertingTask task)
    {
        var msg = new AssetUpdateMessage(
            OriginalPath: task.StoragePath,
            NewUrl: fileStore.GetUrl(task.StorageTargetPath),
            NewPath: null,
            NewName: null,
            NewType: null,
            NewSize: null,
            Progress: 100,
            IsNewAsset: false
        );
        await producer.Produce(AssetTopics.AssetUpdate, JsonSerializer.Serialize(msg));
    }

    private async Task UpdateStatusForMp3(string originalPath, string newPath)
    {
        var metadata = await fileStore.GetMetadata(newPath, CancellationToken.None);
        if (metadata == null)
        {
            logger.LogError("Could not get metadata for converted MP3 file: {NewPath}", newPath);
            return;
        }

        var msg = new AssetUpdateMessage(
            OriginalPath: originalPath,
            NewUrl: fileStore.GetUrl(newPath),
            NewPath: newPath,
            NewName: Path.GetFileName(newPath),
            NewType: metadata.ContentType,
            NewSize: metadata.Size,
            Progress: 100,
            IsNewAsset: true
        );
        await producer.Produce(AssetTopics.AssetUpdate, JsonSerializer.Serialize(msg));
    }
}
