using FormCMS.Core.Files;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface IAssetService
{
    Task EnsureTable();
    XEntity GetEntity(bool countLink);
    string GetBaseUrl();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, bool countLink, CancellationToken ct);
    Task<Asset> Single(long id, CancellationToken ct = default);
    Task<string[]> Add(IFormFile[] files);
    Task Replace(long id, IFormFile file, CancellationToken ct = default);
    Task UpdateMetadata(Asset asset, CancellationToken ct);
    Task UpdateAssetsLinks(string[] newAssetPaths, string entityName, long id, bool checkExisting, CancellationToken ct);
    Task Delete(long id, CancellationToken ct);
}