using FluentResults;
using FormCMS.Infrastructure.ImageUtil;
using Microsoft.AspNetCore.StaticFiles;

namespace FormCMS.Infrastructure.FileStore;

public record LocalFileStoreOptions(string PathPrefix, string UrlPrefix);

public class LocalFileStore(
    LocalFileStoreOptions options
    ):IFileStore
{
    public string GetUrl(string file)=> Path.Join(options.UrlPrefix,file);
    
    public Task<long> GetFileSize(string file)
    {
        string fullPath = Path.Join(options.PathPrefix, file);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(0L);
        }
        
        var sysFileInfo = new FileInfo(fullPath);
        return Task.FromResult(sysFileInfo.Length);
    }
    
    public Task<string> GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        var tp = provider.TryGetContentType(filePath, out var contentType) 
            ? contentType 
            : "application/octet-stream"; // Default fallback
        return Task.FromResult(tp);
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
        var dest = Path.Join(options.PathPrefix, path);
        if (File.Exists(localPath))
        {
            CreateDirAndCopy(localPath, dest);
        }

        return Task.CompletedTask;
    }
    
    public async Task UploadAndDispose(IEnumerable<(string,IFormFile)> files)
    {
        var set = new HashSet<string>();
        foreach (var (fileName, file) in files)
        {
            var dest = Path.Join(options.PathPrefix, fileName);
            var dir = Path.GetDirectoryName(dest);
            
            if (!string.IsNullOrEmpty(dir) && !set.Contains(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                set.Add(dir);
            }

            await using var fileStream = new FileStream(dest, FileMode.Create, FileAccess.Write);
            await file.CopyToAsync(fileStream);
        }
    }
}
