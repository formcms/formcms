using System.Text.Json;
using Confluent.Kafka;
using FormCMS.Auth.ApiClient;
using FormCMS.Core.Assets;
using FormCMS.CoreKit.ApiClient;
using FormCMS.DataLink.Workers;
using FormCMS.Infrastructure.DocumentDbDao;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using HandlebarsDotNet.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xabe.FFmpeg;

namespace FormCMS.Activities.Workers;

public record FFMepgConversionDelayOptions(int DelayMilliseconds);

public sealed class FFMpegWorker : BackgroundService
{
    private readonly ILogger<FFMpegWorker> _logger;
    private readonly IStringMessageConsumer _consumer;
    private readonly LocalFileStoreOptions? _fileStoreOptions;
    private readonly FFMepgConversionDelayOptions _delayOptions;
    private readonly AssetApiClient _assetApiClient;

    public FFMpegWorker(
        ILogger<FFMpegWorker> logger,
        IStringMessageConsumer consumer,
        FFMepgConversionDelayOptions delayOptions,
        LocalFileStoreOptions? fileStoreOptions,
        AssetApiClient apiClient,
        AuthApiClient authApiClient
    )
    {
        ArgumentNullException.ThrowIfNull(fileStoreOptions);
        _logger = logger;
        _assetApiClient = apiClient;
        _consumer = consumer;
        _delayOptions = delayOptions;
        _fileStoreOptions = fileStoreOptions ?? throw new Exception(nameof(fileStoreOptions));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("FFmpeg Worker starting at: {time}", DateTimeOffset.Now);

        // Subscribe to the message topic ONCE
        await _consumer.SubscribeTopic(
            Topics.Rdy4FfMpeg,
            async s =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<FFMpegMessage>(s);
                    if (message is null)
                    {
                        _logger.LogWarning("Could not deserialize message: {RawMessage}", s);
                        return;
                    }

                    if (
                        string.IsNullOrEmpty(message.Path)
                        || string.IsNullOrEmpty(message.TargetFormat)
                    )
                    {
                        _logger.LogWarning(
                            "Invalid message: Missing Path or TargetFormat. Raw: {RawMessage}",
                            s
                        );
                        return;
                    }

                    var path = Path.Join(_fileStoreOptions!.PathPrefix, message.Path);

                    //path = Path.Join(_fileStoreOptions!.PathPrefix, message.Path);
                    if (!File.Exists(path))
                    {
                        _logger.LogWarning("File does not exist at path: {Path}", path);
                        return;
                    }

                    var file = new FileInfo(path);
                    var videoFolder = Path.Join(file.DirectoryName!, "hls");

                    var filesToConvert = new Queue<FileInfo>([file]);
                    await RunConversion(
                        filesToConvert,
                        videoFolder,
                        message.TargetFormat,
                        message.AssetName
                    );

                    _logger.LogInformation(
                        "Processed message. Path={Path}, Format={Format}",
                        message.Path,
                        message.TargetFormat
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing message");
                }
            },
            ct
        );

        // Keep the background service alive until cancellation is requested
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct); // just to keep service running
        }

        _logger.LogInformation("FFmpeg Worker stopping...");
    }

    async Task RunConversion(
        Queue<FileInfo> filesToConvert,
        string outPutFolder,
        string tgtFormat,
        string assetName
    )
    {
        var path = Environment.GetEnvironmentVariable("FFMPEG_EXEC_PATH");
        while (filesToConvert.TryDequeue(out var fileToConvert))
        {
            string outputFileName = Path.Join(
                outPutFolder,
                Path.ChangeExtension(fileToConvert.Name, "." + tgtFormat)
            );
            FFmpeg.SetExecutablesPath(path, ffmpegExeutableName: "ffmpeg");
            var conversion = await FFmpeg.Conversions.FromSnippet.Convert(
                fileToConvert.FullName,
                outputFileName
            );

            await conversion.Start();
            var id = await _assetApiClient.GetAssetIdByName(assetName);
            var asset = await _assetApiClient.Single(id);

            if (asset != null)
            {
                path = outputFileName;
                var assetToUpdated = asset.Value with
                {
                    Url = $"{outPutFolder}/{outputFileName}", //TODO:  Correct as Url
                    Progress = 100,
                };

                await _assetApiClient.UpdateHlsProgress(assetToUpdated);

                _logger.LogInformation($"Finished converion file [{fileToConvert.Name}]");
            }
        }
    }
}
