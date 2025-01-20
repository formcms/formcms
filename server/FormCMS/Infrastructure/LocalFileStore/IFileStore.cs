using FluentResults;

namespace FormCMS.Infrastructure.LocalFileStore;

public interface IFileStore
{
    Task<Result<string[]>> Save(IEnumerable<IFormFile> files);
}