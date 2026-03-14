using NanoidDotNet;

namespace FormCMS.Infrastructure.Downloader;

public class HttpDownloader (IHttpClientFactory factory): IDownloader
{
    private readonly HttpClient _httpClient = factory.CreateClient();
    public async Task<string> DownloadAsync(string url, string destinationPath, CancellationToken ct)
    {
        try
        {
            var shortName = Path.GetFileName(url);
            var fileName= Path.Join(destinationPath, await Nanoid.GenerateAsync(size: 4) + shortName);
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, ct);
            return fileName;
        }
        catch
        {
            return "";
        }
    }
}