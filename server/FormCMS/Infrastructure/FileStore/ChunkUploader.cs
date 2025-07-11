using System.Collections.Concurrent;

namespace FormCMS.Infrastructure.FileStore;

public class ChunkUploader(IFileStore fileStore)
{
    private readonly ConcurrentDictionary<string, List<string>> _uploadProgress = new();

    public async Task<string[]> GetUploadedChunks(string path, CancellationToken ct)
    {
        if (_uploadProgress.TryGetValue(path, out var blockIds))
        {
            return blockIds.ToArray();
        }

        var ids = await fileStore.GetUploadedChunks(path, ct);
        if (ids.Count == 0) return [];
        _uploadProgress.TryAdd(path, ids.ToList());
        return ids.ToArray();
    }

    public async Task CommitChunks(string path, CancellationToken ct)
    {
        var blockIds = await  GetUploadedChunks(path, ct);
        if (blockIds.Length != 0)
        {
            await fileStore.CommitChunks(path, blockIds, ct);
            _uploadProgress.TryRemove(path, out _);
        }
    }
    
    public async Task UploadChunk(string path, int chunkNumber, Stream stream,CancellationToken ct)
    {

        var blockId =await fileStore.UploadChunk(path,chunkNumber, stream, ct);
        _uploadProgress.AddOrUpdate(
            path,
            _ => [blockId],
            (_, list) => { if (!list.Contains(blockId)) list.Add(blockId); return list; });
    }
}