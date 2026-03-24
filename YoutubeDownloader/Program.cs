namespace YoutubeDownloader;
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // === CONFIG ===
        string testUrl = "https://www.youtube.com/watch?v=udrl-sRp_GE"; 
        string destinationFolder = @"/Users/jingshunchen/aaa"; // CHANGE THIS to a real writable folder on your machine

        // Make sure the folder exists
        Directory.CreateDirectory(destinationFolder);

        var downloader = new YoutubeDownloader();

        Console.WriteLine($"Starting download test...");
        Console.WriteLine($"URL: {testUrl}");
        Console.WriteLine($"Saving to: {destinationFolder}");

        try
        {
            var ct = CancellationToken.None; // or use a real CancellationTokenSource for timeout

            string resultFileName = await downloader.DownloadAsync(testUrl, destinationFolder, ct);

            if (string.IsNullOrEmpty(resultFileName))
            {
                Console.WriteLine("Download failed: returned empty filename (possibly unsupported URL or no muxed stream)");
            }
            else
            {
                string fullPath = Path.Join(destinationFolder, resultFileName);
                Console.WriteLine("Success!");
                Console.WriteLine($"Downloaded file: {fullPath}");
                Console.WriteLine($"Size: {new FileInfo(fullPath).Length / 1024 / 1024:F2} MB");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during download:");
            Console.WriteLine(ex.ToString());
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}