using FormCMS.Core.Files;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface IAssetService
{
    Task<Asset> Single(long id, CancellationToken ct = default);
    Task Delete(long id, CancellationToken ct);
    Task<string[]> Add(IFormFile[] files);
    Task EnsureTable();
    XEntity GetEntity(bool countLink);
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, bool countLink, CancellationToken ct);
    string GetBaseUrl();
    Task Replace(string path, IFormFile file);
    Task UpdateAssetsLinks(string[] newAssetPaths, string entityName, long id, bool checkExisting, CancellationToken ct);
}