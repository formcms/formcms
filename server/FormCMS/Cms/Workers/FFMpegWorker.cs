using System.Text.Json;
using Confluent.Kafka;
using FormCMS.DataLink.Workers;
using FormCMS.Infrastructure.DocumentDbDao;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
using HandlebarsDotNet.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xabe.FFmpeg;

namespace FormCMS.Cms.Workers;

public record FFMepgConversionDelayOptions(int DelayMilliseconds);

public sealed class FFMpegWorker : BackgroundService
{
    private readonly ILogger<FFMpegWorker> _logger;
    private readonly IStringMessageConsumer _consumer;
    private readonly LocalFileStoreOptions? _fileStoreOptions;
    private readonly FFMepgConversionDelayOptions _delayOptions;

    public FFMpegWorker(
        ILogger<FFMpegWorker> logger,
        IStringMessageConsumer consumer,
        FFMepgConversionDelayOptions delayOptions,
        LocalFileStoreOptions? fileStoreOptions
    )
    {
        ArgumentNullException.ThrowIfNull(fileStoreOptions);
        _logger = logger;

        _consumer = consumer;
        _delayOptions = delayOptions;
        _fileStoreOptions = fileStoreOptions ?? throw new Exception(nameof(fileStoreOptions));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(" FFMpeg Worker running at: {time}", DateTimeOffset.Now);
            }
            await _consumer.SubscribeTopic(
                Topics.Rdy4FfMpeg,
                async s =>
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<FFMpegMessage>(s);
                        if (message is null)
                        {
                            _logger.LogWarning("Could not deserialize message");
                            return;
                        }

                        if (
                            string.IsNullOrEmpty(message.Path)
                            || string.IsNullOrEmpty(message.TargetFormat)
                        )
                        {
                            _logger.LogWarning(
                                "{message} missing path or targetFormat , ignore the message",
                                s
                            );
                        }

                        var path = Path.Join(_fileStoreOptions!.PathPrefix, message.Path);
                        if (File.Exists(path))
                        {
                            var file = new FileInfo(path);

                            var videoFolder = file.DirectoryName + "/hls";

                            var filesToConvert = new Queue<FileInfo>([file]);
                            await RunConversion(filesToConvert, videoFolder, message.TargetFormat);
                        }

                        _logger.LogInformation(
                            "consumed message successfully, path ={message.Path}, targetFormat={message.TargetFormat}",
                            message.Path,
                            message.TargetFormat
                        );
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Fail to handler message, err= {error}", e.Message);
                    }
                    await Task.Delay(_delayOptions.DelayMilliseconds * 1000, ct);
                },
                ct
            );
        }
    }

    async Task RunConversion(Queue<FileInfo> filesToConvert, string outPutFolder, string tgtFormat)
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
            await Console.Out.WriteLineAsync($"Finished converion file [{fileToConvert.Name}]");
        }
    }
}
