using System.Text;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class ChunkUploadApiTest(AppFactory factory)
{
    string  fileName = $"{Ulid.NewUlid()}.txt";
    const int bufferSize = 2;
    private string data = "1234567";
    private bool _ = factory.LoginAndInitTestData();

    [Fact]
    public async Task ChunkStatus()
    {
        var status = await factory.ChunkUploadApiClient.ChunkStatus(fileName, data.Length).Ok();
        Assert.NotNull(status);
    }
    [Fact]
    public async Task ResumeUploadAndCommitChunk()
    {
        var status  = await factory.ChunkUploadApiClient.ChunkStatus(fileName, data.Length).Ok();
        var bs = Encoding.UTF8.GetBytes("12");
        await factory.ChunkUploadApiClient.UploadChunk(status.Path, "text/html",0, bs).Ok();
        
        status  = await factory.ChunkUploadApiClient.ChunkStatus(fileName, data.Length).Ok();
        for (var i = status.Count; i * bufferSize< data.Length; i++)
        {
            var s = data.Substring(i * bufferSize, Math.Min(bufferSize,data.Length - i * bufferSize));
            bs = Encoding.UTF8.GetBytes(s);
            await factory.ChunkUploadApiClient.UploadChunk(status.Path, "text/html",i, bs).Ok();
        }
        
        await factory.ChunkUploadApiClient.Commit(status.Path, fileName).Ok();
        
        var asset = await factory.AssetApi.Single(status.Path).Ok();
        Assert.NotNull(asset);
    }
    [Fact]
    public async Task UploadAndCommitChunk()
    {
        var status  = await factory.ChunkUploadApiClient.ChunkStatus(fileName, data.Length).Ok();

        for (var i = status.Count; i * bufferSize< data.Length; i++)
        {
            var s = data.Substring(i * bufferSize, Math.Min(bufferSize,data.Length - i * bufferSize));
            var bs = Encoding.UTF8.GetBytes(s);
            await factory.ChunkUploadApiClient.UploadChunk(status.Path, "text/html",i, bs).Ok();
        }
        
        await factory.ChunkUploadApiClient.Commit(status.Path, fileName).Ok();
        
        var asset = await factory.AssetApi.Single(status.Path).Ok();
        Assert.NotNull(asset);
    }
}