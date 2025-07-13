using FluentResults;
using FormCMS.Core.Assets;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.CoreKit.ApiClient;

public class ChunkUploadApiClient(HttpClient client)
{
    public async Task<Result> UploadChunk(string fileId, string type, int chunkNumber, byte[] bytes)
    {
        var url = "/".ToChunkUploadApi();
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(type);
        content.Add(fileContent, "files", "chunk");
        content.Add(new StringContent(fileId), "fileId");
        content.Add(new StringContent(chunkNumber.ToString()), "chunkNumber");
        var msg = await client.PostAsync(url,content);
        return await msg.ParseResult(); 
    }

    public Task<Result<ChunkStatus>> ChunkStatus(string fileName, long size)
        => client.GetResult<ChunkStatus>($"/status?fileName={fileName}&size={size}".ToChunkUploadApi());

    public async Task<Result> Commit(string fileId, string fileName)
    {
        var url = "/commit".ToChunkUploadApi();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(fileId), "fileId");
        content.Add(new StringContent(fileName), "fileName");
        var msg = await client.PostAsync(url,content);
        return await msg.ParseResult(); 
    }
}