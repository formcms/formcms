using FormCMS.Core.Assets;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface IAssetService
{
    XEntity GetEntity(bool withLinkCount);
    string GetBaseUrl();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, bool withLinkCount, CancellationToken ct);
    Task<Asset> Single(long id, bool loadLinks, CancellationToken ct );
    Task<Asset> Single(string path, bool loadLinks, CancellationToken ct );
    Task<string[]> BatchUploadAndAdd(IFormFile[] files,CancellationToken ct);
    Task Replace(long id, IFormFile file, CancellationToken ct );
    Task UpdateMetadata(Asset asset, CancellationToken ct);
    Task UpdateAssetsLinks(Record[]oldLinks, string[] newAssets, string entityName, long id, CancellationToken ct);
    Task Delete(long id, CancellationToken ct);
    Task UpdateHlsProgress(Asset asset, CancellationToken ct);
    Task<string> AddWithAction(string path, string fileName, CancellationToken ct);
    bool IsValidSignature(IFormFile file);
}