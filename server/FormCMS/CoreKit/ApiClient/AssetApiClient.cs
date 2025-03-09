using FluentResults;
using FormCMS.Core.Assets;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.CoreKit.ApiClient;

public class AssetApiClient(HttpClient client)
{
    public Task<Result<string>> GetEntityBaseUrl()
        => client.GetStringResult($"/base".ToAssetApi());
    
    public Task<Result<XEntity>> GetEntity(bool withLinkCount)
        => client.GetResult<XEntity>($"/entity?linkCount={withLinkCount}".ToAssetApi());
    
    public Task<Result<string>> AddAsset(IEnumerable<(string, byte[])> files)
        => client.PostFileResult("/".ToAssetApi(), "files", files);

    public Task<Result<ListResponse>> List(bool withLinkCount, string qs)
        => client.GetResult<ListResponse>($"/?linkCount={withLinkCount}&{qs}".ToAssetApi());
    
    public Task<Result<Asset>> Single(long id)
        => client.GetResult<Asset>($"/{id}".ToAssetApi());

    public async Task<Result> Replace(long id, string fileName, byte[] fileContent)
    {
       var res = await client.PostFileResult($"/{id}".ToAssetApi(),"files",[(fileName, fileContent)]);
       return res.IsFailed ? Result.Fail(res.Errors) : Result.Ok();
    }
     public Task<Result> UpdateMeta(Asset asset)
         => client.PostResult($"/meta".ToAssetApi(), asset);
}