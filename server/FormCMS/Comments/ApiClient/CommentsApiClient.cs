using FluentResults;
using FormCMS.Comments.Models;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Comments.ApiClient;

public class CommentsApiClient(HttpClient client)
{
    public Task<Result<Comment>> Add(Comment comment)
        => client.GetResult<Comment>($"/".Url());
    
    public Task<Result> Update(Comment comment)
        => client.GetResult($"/update".Url());
    
    public Task<Result> Delete(long id)
        => client.GetResult($"/delete/{id}".Url());
}