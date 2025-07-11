using System.Text;
using Microsoft.AspNetCore.StaticFiles;

namespace FormCMS.Infrastructure.FileStore;

public record LocalFileStoreOptions(string PathPrefix, string UrlPrefix);

public class LocalFileStore(
    LocalFileStoreOptions options
) : IFileStore
{
    private readonly FileExtensionContentTypeProvider _provider = new();

    public Task Upload(string localPath, string path, CancellationToken ct)
    {
        var dest = Path.Join(options.PathPrefix, path);
        if (File.Exists(localPath))
        {
            CreateDirAndCopy(localPath, dest);
        }

        return Task.CompletedTask;
    }

    public async Task Upload(IEnumerable<(string, IFormFile)> files, CancellationToken ct)
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


    public Task<FileMetadata?> GetMetadata(string filePath, CancellationToken ct)
    {
        string fullPath = Path.Join(options.PathPrefix, filePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<FileMetadata?>(null);
        }

        var sysFileInfo = new FileInfo(fullPath);
        var ret = new FileMetadata(sysFileInfo.Length, GetContentType(filePath));
        return Task.FromResult<FileMetadata?>(ret);
    }

    public string GetUrl(string file) => Path.Join(options.UrlPrefix, file);

    public Task Download(string path, string localPath, CancellationToken ct)
    {
        path = Path.Join(options.PathPrefix, path);
        if (File.Exists(path))
        {
            CreateDirAndCopy(path, localPath);
        }

        return Task.CompletedTask;
    }

    public async Task DownloadFileWithRelated(string path, string localPath, CancellationToken ct)
    {
        var sourceRoot = options.PathPrefix.TrimEnd('/');
        var fullFilePath = Path.Join(sourceRoot, path);
        await Download(path, localPath, ct);
        

        var parentDir = Path.GetDirectoryName(fullFilePath) ?? sourceRoot;
        var filePrefix = Path.GetFileName(path);
        var matchingDirs = Directory.EnumerateDirectories(parentDir, $"{filePrefix}*", SearchOption.TopDirectoryOnly);

        foreach (var matchingDir in matchingDirs)
        {
            var files = Directory.GetFiles(matchingDir, "*", SearchOption.AllDirectories);

            foreach (var sourceFile in files)
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(fullFilePath, sourceFile);
                var destPath = Path.Combine(localPath, relativePath);
                CreateDirAndCopy(sourceFile, destPath);
            }
        }

    }


    public Task Del(string file, CancellationToken ct)
    {
        file = Path.Join(options.PathPrefix, file);
        File.Delete(file);
        return Task.CompletedTask;
    }
    
    public Task DelByPrefix(string prefix, CancellationToken ct)
    {
        var fullPrefixPath = Path.Combine(options.PathPrefix, prefix);

        if (Directory.Exists(fullPrefixPath))
        {
            Directory.Delete(fullPrefixPath, recursive: true);
        }

        return Task.CompletedTask;
    }

     public async Task<string> UploadChunk(string blobName, int chunkNumber, Stream stream, CancellationToken ct)
    {
        var chunkDir = Path.Join(options.PathPrefix, "chunks", blobName);
        Directory.CreateDirectory(chunkDir);
        var chunkPath = Path.Combine(chunkDir, $"{chunkNumber:D6}");
        var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{chunkNumber:D6}"));

        await using var fileStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);

        return blockId;
    }

    public async Task<List<string>> GetUploadedChunks(string blobName, CancellationToken ct)
    {
        var chunkDir = Path.Join(options.PathPrefix, "chunks", blobName);
        if (!Directory.Exists(chunkDir))
        {
            return [];
        }

        var chunkFiles = Directory.GetFiles(chunkDir, "[0-9][0-9][0-9][0-9][0-9][0-9]")
            .Select(f => int.Parse(Path.GetFileName(f)))
            .OrderBy(n => n)
            .Select(n => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{n:D6}")))
            .ToList();

        return chunkFiles;
    }

    public async Task CommitChunks(string blobName, IEnumerable<string> blockIds, CancellationToken ct)
    {
        var finalPath = Path.Join(options.PathPrefix, blobName);
        var chunkDir = Path.Join(options.PathPrefix, "chunks", blobName);

        if (!Directory.Exists(chunkDir)) return;

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ?? string.Empty);
        await using var finalStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write);

        foreach (var blockId in blockIds.OrderBy(id => int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(id)))))
        {
            var chunkNumber = int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(blockId)));
            var chunkPath = Path.Combine(chunkDir, $"{chunkNumber:D6}");
            if (File.Exists(chunkPath))
            {
                await using var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                await chunkStream.CopyToAsync(finalStream, ct);
            }
        }

        Directory.Delete(chunkDir, recursive: true);
    }
    private string GetContentType(string filePath)
        => _provider.TryGetContentType(filePath, out var contentType)
            ? contentType
            : "application/octet-stream"; // Default fallback


    private void CreateDirAndCopy(string source, string dest)
    {
        FileUtils.EnsureParentFolder(dest);
        File.Copy(source, dest, true);
    }
}