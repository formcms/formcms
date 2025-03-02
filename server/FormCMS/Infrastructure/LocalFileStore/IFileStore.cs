using FluentResults;

namespace FormCMS.Infrastructure.LocalFileStore;

public record FileInfo(string Path, string Url, string Name, string ContentType, int FileSize);
public interface IFileStore
{
    Task<Result<FileInfo[]>> Upload(IEnumerable<IFormFile> files);
    Task Upload(string localPath, string path);
    Task Download(string path, string localPath);
    Task Del(string file);
    string GetDownloadPath(string file);
}