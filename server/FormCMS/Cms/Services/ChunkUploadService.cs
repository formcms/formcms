using FormCMS.Core.Assets;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Cms.Services;

public class ChunkUploadService(
    IIdentityService identityService,
    IAssetService assetService,
    ShardGroup shardGroup,
    IFileStore fileStore
    ):IChunkUploadService
{
    
    public async Task UploadChunk(string path, int number, IFormFile file, CancellationToken ct)
    {
        if (identityService.GetUserAccess()?.CanAccessAdmin != true) throw new ResultException("User not found");
        if (number == 0 && !assetService.IsValidSignature(file))
        {
            throw new ResultException("Invalid file signature");
        }
        await using var stream = file.OpenReadStream();
        await fileStore.UploadChunk(path, number, stream, ct);
    }

    public async Task<ChunkStatus> ChunkStatus(string fileName, long fileSize, CancellationToken ct)
    {
        if (identityService.GetUserAccess()?.CanAccessAdmin != true) throw new ResultException("User not found");
        var userId = identityService.GetUserAccess()!.Id; 
        var session = new UploadSession(userId, fileName, fileSize, FileUtils.GetFilePath(fileName));
        var record = await shardGroup.PrimaryDao.Single(UploadSessions.Find(userId, fileName, fileSize),ct);
        if (record is null)
        {
            await shardGroup.PrimaryDao.Exec(session.Insert(), ct);
            return new ChunkStatus(session.Path, 0);
        }
        
        var path = record.StrOrEmpty(nameof(UploadSession.Path).Camelize());
        var chunks = await fileStore.GetUploadedChunks(path,ct);
        return new ChunkStatus(path, chunks.Length);
    }

    public async Task Commit(string path, string fileName,  CancellationToken ct)
    {
        if (identityService.GetUserAccess()?.CanAccessAdmin != true) throw new ResultException("User not found");
        await fileStore.CommitChunks(path, ct);
        await assetService.AddWithAction(path, fileName, ct);
        await shardGroup.PrimaryDao.Exec(UploadSessions.Delete(path), ct);
    }
}