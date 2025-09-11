using FormCMS.Core.Assets;

namespace FormCMS.Cms.Services;

public interface IChunkUploadService
{
    Task UploadChunk(string path, int number, IFormFile file, CancellationToken ct);
    Task<ChunkStatus> ChunkStatus(string fileName, long fileSize, CancellationToken ct);
    Task Commit(string path, string fileName, CancellationToken ct);
}