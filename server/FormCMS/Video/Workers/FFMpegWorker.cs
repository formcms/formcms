using System.Text.Json;
using FormCMS.Core.Assets;
using FormCMS.Core.Auth;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Utils.ResultExt;
using FormCMS.Video.Models;
using Xabe.FFmpeg;

namespace FormCMS.Video.Workers;

public sealed class FFMpegWorker(
    ILogger<FFMpegWorker> logger,
    IStringMessageConsumer consumer,
    CmsRestClientSettings  restClientSettings,
    IFileStore fileStore
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("FFmpeg Worker starting at: {time}", DateTimeOffset.Now);

        // Subscribe to the message topic ONCE
        await consumer.Subscribe(
            VideoTopics.Rdy4FfMpeg,
            async s =>
            {
                try
                {
                    var msg = ParseMessage(s);
                    if (msg is null) return;

                    var task = HlsConvertingTaskHelper.CreatTask(msg.Path);
 
                    if (msg.IsDelete)
                    {
                        await fileStore.DelByPrefix(task.StorageFolder, ct);
                    }
                    else
                    {
                        await DoConvert(task); 
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

    private async Task DoConvert(HlsConvertingTask task)
    {
        await fileStore.Download(task.StoragePath, task.TempPath, CancellationToken.None);
        await RunConversion(task);
        await fileStore.UploadFolder(task.TempFolder, task.StorageFolder,CancellationToken.None);
        File.Delete(task.TempPath);
        Directory.Delete(task.TempFolder, recursive: true);
        await UpdateStatus(task);
        logger.LogInformation( "Processed message. Task={task}", task);       
    }

    private FFMpegMessage? ParseMessage(string s)
    {
        var message = JsonSerializer.Deserialize<FFMpegMessage>(s);
        if (message is null)
        {
            logger.LogWarning("Could not deserialize message: {RawMessage}", s);
            return null;
        }

        if (
            string.IsNullOrEmpty(message.Path)
            || string.IsNullOrEmpty(message.TargetFormat)
            || message.TargetFormat != "m3u8"
        )
        {
            logger.LogWarning(
                "Invalid message: Missing Path or TargetFormat. Raw: {RawMessage}",
                s
            );
            return null;
        }
        return message;

    }
    
    private async Task RunConversion(HlsConvertingTask task)
    {
        // Directory.CreateDirectory(task.TempFolder);
        var path = Environment.GetEnvironmentVariable("FFMPEG_EXEC_PATH");
        FFmpeg.SetExecutablesPath(path, ffmpegExeutableName: "ffmpeg");
        var conversion = await FFmpeg.Conversions.FromSnippet.Convert(
            task.TempPath,
            task.TempTargetPath
        );

        await conversion.Start();
        logger.LogInformation("Finished conversion file [{TaskStoragePath}]", task.StoragePath);
    }

    private async Task UpdateStatus(HlsConvertingTask task)
    {
        var client=new HttpClient();
        client.BaseAddress = new Uri(restClientSettings.BaseUrl );
        client.DefaultRequestHeaders.Add( "X-Cms-Adm-Api-Key", restClientSettings.ApiKey);
        
        var assetApiClient = new AssetApiClient(client);
        var assetToUpdated = new Asset
        (
            Path : task.StoragePath,
            Url : fileStore.GetUrl(task.StorageTargetPath), 
            Progress : 100
        );
        await assetApiClient.UpdateHlsProgress(assetToUpdated).Ok();
    }
}
