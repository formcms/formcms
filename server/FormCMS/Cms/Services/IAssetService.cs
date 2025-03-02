using FormCMS.Utils.DisplayModels;

namespace FormCMS.Cms.Services;

public interface IAssetService
{
    Task<string[]> Add(IEnumerable<IFormFile> files);
    Task EnsureTable();
    XEntity GetEntity();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct);
    string GetBaseUrl();
}