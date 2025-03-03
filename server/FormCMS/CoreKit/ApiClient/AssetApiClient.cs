using FluentResults;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.CoreKit.ApiClient;

public class AssetApiClient(HttpClient client)
{
    public Task<Result<string>> AddAsset(IEnumerable<(string, byte[])> files)
        => client.PostFileResult("/".ToAssetApi(), "files", files);

    public Task<Result<ListResponse>> List(string qs)
        => client.GetResult<ListResponse>($"/?{qs}".ToAssetApi());
}