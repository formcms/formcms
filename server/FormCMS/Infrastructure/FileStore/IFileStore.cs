namespace FormCMS.Infrastructure.FileStore;

public record FileMetadata(long Size, string ContentType);

public interface IFileStore
{
    Task Upload(IEnumerable<(string,IFormFile)> files, CancellationToken ct);
    Task Upload(string localPath, string path, CancellationToken ct);
    Task<FileMetadata?> GetMetadata(string filePath, CancellationToken ct);
    string GetUrl(string file);
    Task Download(string path, string localPath, CancellationToken ct);
    Task DownloadFileWithRelated(string path, string localPath, CancellationToken ct);
    Task Del(string file, CancellationToken ct);
    Task DelByPrefix(string prefix, CancellationToken ct);
}

public static class FileStoreExtensions 
{
    public static async Task UploadFileAndRelated(this IFileStore fileStore, string localPath, string path, CancellationToken ct)
    {
        if (File.Exists(localPath))
        {
            await fileStore.Upload(localPath, path, ct);
        }

        var parentDir = Path.GetDirectoryName(localPath)!;
        var filePrefix = Path.GetFileName(localPath);
        var matchingDirs = Directory.EnumerateDirectories(parentDir, $"{filePrefix}*", SearchOption.TopDirectoryOnly);

        foreach (var matchingDir in matchingDirs)
        {
            var relativePath = matchingDir[localPath.Length..];
            var destPath = path + relativePath;
            await fileStore.UploadFolder(matchingDir, destPath, ct);
        }
    }
    
    public static async Task UploadFolder(this IFileStore fileStore, string localPath, string path, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileStore);
        if (string.IsNullOrWhiteSpace(localPath))
            throw new ArgumentException("Local path cannot be null or empty.", nameof(localPath));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Destination path cannot be null or empty.", nameof(path));
        if (!Directory.Exists(localPath))
            throw new DirectoryNotFoundException($"Local folder '{localPath}' does not exist.");

        localPath = Path.GetFullPath(localPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        path = path.TrimEnd('/', '\\').Replace('\\', '/'); // Ensure forward slashes for file store compatibility

        var files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = file[(localPath.Length + 1)..].Replace('\\', '/'); // Remove localPath prefix and normalize slashes
            var destinationPath = $"{path}/{relativePath}"; // Combine with destination path
            await fileStore.Upload(file, destinationPath, ct);
        }
    }
}