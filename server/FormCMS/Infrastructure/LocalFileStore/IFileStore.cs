using FluentResults;

namespace FormCMS.Infrastructure.LocalFileStore;

public interface IFileStore
{
    Task<Result<string[]>> Save(IEnumerable<IFormFile> files);
    void Move(string fromPath, string toPath);
    void Del(string file);
    string GetDownloadPath(string file);
}