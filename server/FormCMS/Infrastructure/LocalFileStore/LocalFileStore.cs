using FluentResults;
using FormCMS.Infrastructure.ImageUtil;
using Microsoft.AspNetCore.Server.HttpSys;
using NUlid;

namespace FormCMS.Infrastructure.LocalFileStore;

public record LocalFileStoreOptions(string PathPrefix, string UrlPrefix);
public class LocalFileStore(
    LocalFileStoreOptions options,
    Resizer resizer
    ):IFileStore
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
    
    public async Task<Result<FileInfo[]>> Upload(IEnumerable<IFormFile> files)
    {
        var dir = GetDirectoryName();
        Directory.CreateDirectory(Path.Join(options.PathPrefix, dir));
        var ret = new List<FileInfo>();

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                return Result.Fail($"Invalid file length {file.FileName}");
            }

            var filePath = "/" + Path.Join(dir, GetUniqName(file.FileName));
            await using var saveStream = File.Create(Path.Join(options.PathPrefix, filePath));

            if (resizer.IsImage(file))
            {
                resizer.Compress(file.OpenReadStream(),saveStream);
            }
            else
            {
                await file.CopyToAsync(saveStream);
            }

            ret.Add(new FileInfo(
                Path: filePath,  // Full filesystem path
                Url: options.UrlPrefix + filePath,            // URL for access
                Name: file.FileName,
                ContentType: file.ContentType,
                FileSize: (int)saveStream.Length
            ));
        }

        return ret.ToArray();
    }

   

    private string GetUniqName(string fileName)
        => string.Concat(Ulid.NewUlid().ToString().AsSpan(0, 12), Path.GetExtension(fileName));

    private string GetDirectoryName() => DateTime.Now.ToString("yyyy-MM");
}
