namespace FormCMS.Infrastructure.Downloader;

public interface IDownloader
{
    Task<string> DownloadAsync(string url, string destinationPath, CancellationToken ct);
}