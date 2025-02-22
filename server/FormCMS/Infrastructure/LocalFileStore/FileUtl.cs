using System.Net;
using FluentResults;
using SkiaSharp; 

namespace FormCMS.Infrastructure.LocalFileStore;

public record LocalFileStoreOptions(string PathPrefix, string UrlPrefix, int MaxImageWith, int Quality);
public class LocalFileStore(LocalFileStoreOptions options):IFileStore
{
    public string GetDownloadPath(string file)=>$"{options.UrlPrefix}/{file}";

    public Task Download(string path, string localPath)
    {
        path = Path.Join(options.PathPrefix, path);
        if (File.Exists(path))
        {
            CreateDirAndCopy(path, localPath);
        }

        return Task.CompletedTask;
    }

    public Task Del(string file)
    {
        file = Path.Join(options.PathPrefix, file);
        File.Delete(file);
        return Task.CompletedTask;
    }

    private void CreateDirAndCopy(string source, string dest)
    {
        string? destinationDir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        File.Copy(source, dest, true);
    }

    public Task Upload(string localPath, string path)
    {
        path = Path.Join(options.PathPrefix, path);
        if (File.Exists(localPath))
        {
            CreateDirAndCopy(localPath, path);
        }
        return Task.CompletedTask;
    }
    
    public async Task<Result<string[]>> Upload(IEnumerable<IFormFile> files)
    {
        var dir = GetDirectoryName();
        Directory.CreateDirectory(Path.Join(options.PathPrefix, dir));
        List<string> ret = new();

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                return Result.Fail($"Invalid file length {file.FileName}");
            }

            var fileName = Path.Join(dir, GetFileName(file.FileName));
            await using var saveStream = File.Create(Path.Join(options.PathPrefix, fileName));

            if (IsImage(file))
            {
                await using var inStream = file.OpenReadStream();
                using var originalBitmap = SKBitmap.Decode(inStream);
                if (originalBitmap.Width > options.MaxImageWith)
                {
                    CompressImage(originalBitmap, saveStream );
                }
                else
                {
                    await file.CopyToAsync(saveStream);
                }
            }
            else
            {
                await file.CopyToAsync(saveStream);
            }
            ret.Add("/" + fileName);
        }

        return ret.ToArray();
    }

    private bool IsImage(IFormFile file)
    {
        string[] validExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];
        var ext = Path.GetExtension(file.FileName).ToLower();
        return validExtensions.Contains(ext);
    }

    private void CompressImage(SKBitmap originalBitmap, Stream outStream)
    {
        var scaleFactor = (float)options.MaxImageWith / originalBitmap.Width;
        var newHeight = (int)(originalBitmap.Height * scaleFactor);

        var resizedImage = originalBitmap.Resize(new SKImageInfo(options.MaxImageWith, newHeight), SKSamplingOptions.Default);
        resizedImage?.Encode(outStream, SKEncodedImageFormat.Jpeg, options.Quality);
    }

    private string GetFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var randomString = Guid.NewGuid().ToString("N")[..8];
        return randomString + ext;
    }

    private string GetDirectoryName()
    {
        return DateTime.Now.ToString("yyyy-MM");
    }
}
