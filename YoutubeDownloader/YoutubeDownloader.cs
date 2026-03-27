using FormCMS.Infrastructure.Downloader;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;

namespace YoutubeDownloader;

public class YoutubeDownloader : IDownloader
{
    public async Task<string> DownloadAsync(string url, string destinationPath, CancellationToken ct)
    {
        if (!url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) &&
            !url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        var youtube = new YoutubeClient();

        var video = await youtube.Videos.GetAsync(url, ct);

        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id, ct);

        var videoStreamInfo = streamManifest
            .GetVideoStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestVideoQuality();

        var audioStreamInfo = streamManifest
            .GetAudioStreams()
            .GetWithHighestBitrate();

        if (videoStreamInfo == null || audioStreamInfo == null)
        {
            var muxedStreamInfo = streamManifest
                .GetMuxedStreams()
                .GetWithHighestVideoQuality();
            
            if (muxedStreamInfo == null)
                return "";
                
            var fileName = $"{video.Title}.{muxedStreamInfo.Container}";
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Join(destinationPath, fileName);
            await youtube.Videos.Streams.DownloadAsync(muxedStreamInfo, path, cancellationToken: ct);
            return fileName;
        }

        var fileNameCombined = $"{video.Title}.mp4";
        fileNameCombined = string.Join("_", fileNameCombined.Split(Path.GetInvalidFileNameChars()));
        var pathCombined = Path.Join(destinationPath, fileNameCombined);
        
        var streamInfos = new [] { audioStreamInfo, videoStreamInfo };
        await youtube.Videos.DownloadAsync(streamInfos, new ConversionRequestBuilder(pathCombined).Build(), cancellationToken: ct);
        
        return pathCombined;
    }
}