using System.Text.Json;
using FormCMS.Cms.Models;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Video.Models;
using Xabe.FFmpeg;

namespace FormCMS.Video.Workers;

public class HlsConversionStrategy(
    ILogger logger,
    IFileStore fileStore,
    IStringMessageProducer producer
) : IConversionStrategy
{
    public bool CanHandle(ConvertVideoMessage message) => message.TargetPath?.EndsWith($".{ConvertVideoFormats.M3u8}") == true;

    public async Task ExecuteAsync(ConvertVideoMessage message, CancellationToken ct)
    {
        var task = HlsConvertingTaskHelper.CreatTask(message.Path);
        await fileStore.Download(task.StoragePath, task.TempPath, ct);
        
        var path = Environment.GetEnvironmentVariable("FFMPEG_EXEC_PATH");
        FFmpeg.SetExecutablesPath(path, ffmpegExeutableName: "ffmpeg");
        
        var conversion = await FFmpeg.Conversions.FromSnippet.Convert(task.TempPath, task.TempTargetPath);
        await conversion.Start(ct);
        
        await fileStore.UploadFolder(task.TempFolder, task.StorageFolder, ct);
        File.Delete(task.TempPath);
        Directory.Delete(task.TempFolder, recursive: true);
        
        await UpdateStatus(task);
        logger.LogInformation("HLS conversion finished for [{TaskStoragePath}]", task.StoragePath);
    }

    private async Task UpdateStatus(HlsConvertingTask task)
    {
        var msg = new AssetUpdateMessage(
            NewPath: task.StorageTargetPath,
            Progress: 100
        );
        await producer.Produce(AssetTopics.AssetUpdate, JsonSerializer.Serialize(msg));
    }
}
