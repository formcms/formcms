namespace FormCMS.Infrastructure.FileStore;

public record FileMetadata(long Size, string ContentType);

public interface IFileStore
{
    Task Upload(IEnumerable<(string,IFormFile)> files, CancellationToken ct);
    Task Upload(string localPath, string path, CancellationToken ct);
    Task<FileMetadata?> GetMetadata(string filePath, CancellationToken ct);
    string GetUrl(string file);
    Task Download(string path, string localPath, CancellationToken ct);
    Task Del(string file, CancellationToken ct);
}