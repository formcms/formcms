using FluentResults;
using FormCMS.Comments.Models;
using FormCMS.Utils.HttpClientExt;

namespace FormCMS.Comments.ApiClient;

public class CommentsApiClient(HttpClient client)
{
    public Task<Result<Comment>> Add(Comment comment)
        => client.PostResult<Comment>($"/".Url(),comment);
    
    public Task<Result> Update(Comment comment)
        => client.PostResult($"/update".Url(),comment);
    
    public Task<Result> Delete(long id)
        => client.PostResult($"/delete/{id}".Url(),new{});

    public Task<Result> Reply(long id, Comment comment)
        => client.PostResult($"/reply/{id}".Url(), comment);
}