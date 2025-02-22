using FluentResults;

namespace FormCMS.Infrastructure.LocalFileStore;

public interface IFileStore
{
    Task<Result<string[]>> Upload(IEnumerable<IFormFile> files);
    Task Upload(string localPath, string path);
    Task Download(string path, string localPath);
    Task Del(string file);
    string GetDownloadPath(string file);
}