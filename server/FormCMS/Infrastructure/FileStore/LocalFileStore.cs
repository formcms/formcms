using FluentResults;
using FormCMS.Infrastructure.ImageUtil;

namespace FormCMS.Infrastructure.FileStore;

public record LocalFileStoreOptions(string PathPrefix, string UrlPrefix);

public class LocalFileStore(
    LocalFileStoreOptions options,
    Resizer resizer
    ):IFileStore
{
    public string GetUrl(string file)=> Path.Join(options.UrlPrefix,file);
    
    public Task<FileInfo?> GetFileInfo(string file)
    {
        string fullPath = Path.Join(options.PathPrefix, file);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<FileInfo?>(null);
        }

        var sysFileInfo = new System.IO.FileInfo(fullPath);
        var fileInfo = new FileInfo(
            Path: fullPath,
            Url: GetUrl(file),
            Name: sysFileInfo.Name,
            ContentType: Util.GetContentType(sysFileInfo.Extension),
            FileSize: (int)sysFileInfo.Length // Cast long to int, assuming files < 2GB
        );

        return Task.FromResult<FileInfo?>(fileInfo);
    }

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
        var dir = Util.GetDirectoryName();
        Directory.CreateDirectory(Path.Join(options.PathPrefix, dir));
        var ret = new List<FileInfo>();

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                return Result.Fail($"Invalid file length {file.FileName}");
            }

            var filePath = "/" + Path.Join(dir, Util.GetUniqName(file.FileName));
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
                Url: GetUrl(filePath),            // URL for access
                Name: file.FileName,
                ContentType: file.ContentType,
                FileSize: (int)saveStream.Length
            ));
        }

        return ret.ToArray();
    }
}
