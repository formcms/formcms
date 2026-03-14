using FormCMS.Infrastructure.Downloader;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

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

        var streamInfo = streamManifest
            .GetMuxedStreams()
            .GetWithHighestVideoQuality();

        if (streamInfo == null)
            return "";

        var fileName = $"{video.Title}.{streamInfo.Container}";
        // Clean filename from invalid characters
        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Join(destinationPath, fileName);
        await using var stream = await youtube.Videos.Streams.GetAsync(streamInfo, ct);
        await using var file = File.Create(path);
        await stream.CopyToAsync(file, ct);
        return path;
    }
}