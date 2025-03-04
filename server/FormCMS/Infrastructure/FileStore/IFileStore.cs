using FluentResults;

namespace FormCMS.Infrastructure.FileStore;

public record FileInfo(string Path, string Url, string Name, string ContentType, int FileSize);
public interface IFileStore
{
    Task<Result<FileInfo[]>> Upload(IEnumerable<IFormFile> files);
    Task Upload(string localPath, string path);
    Task Download(string path, string localPath);
    Task Del(string file);
    string GetUrl(string file);
    Task<FileInfo?> GetFileInfo(string file);
}