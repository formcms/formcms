using FormCMS.Core.Assets;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface IAssetService
{
    Task EnsureTable();
    XEntity GetEntity(bool withLinkCount);
    string GetBaseUrl();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, bool withLinkCount, CancellationToken ct);
    Task<Asset> Single(long id, bool loadLinks, CancellationToken ct = default);
    Task<Asset> Single(string path, bool loadLinks, CancellationToken ct = default);
    Task<string[]> Add(IFormFile[] files);
    Task Replace(long id, IFormFile file, CancellationToken ct = default);
    Task UpdateMetadata(Asset asset, CancellationToken ct);
    Task UpdateAssetsLinks(Record[]oldLinks, string[] newAssets, string entityName, long id, CancellationToken ct);
    Task Delete(long id, CancellationToken ct);
}