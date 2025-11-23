using System.Text.Json;
using FormCMS.Comments.Models;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using Microsoft.Extensions.Primitives;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class CommentApiTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();
    private readonly string _queryName = "qry_query_" + Ulid.NewUlid();

    [Fact]
    public async Task AddComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: BlogsTestData.CommentTestPostId,
            CreatedBy:"",
            Content:"test"
            );
        await factory.CommentsApiClient.Add(comment).Ok();
        var count = await QueryCommentsCount();
        Assert.True(count > 0);
    }
    
  
    
    [Fact]
    public async Task DeleteComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: BlogsTestData.CommentTestPostId,
            CreatedBy:"",
            Content:"test"
        );
        comment = await factory.CommentsApiClient.Add(comment).Ok();
        var count = await QueryCommentsCount();
        await factory.CommentsApiClient.Delete(comment.Id).Ok();
        var countAfter = await QueryCommentsCount();
        Assert.NotEqual(count, countAfter);
    }
    
    [Fact]
    public async Task UpdateComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: BlogsTestData.CommentTestPostId,
            CreatedBy:"",
            Content:"test"
        );
        comment = await factory.CommentsApiClient.Add(comment).Ok();
        await factory.CommentsApiClient.Update(comment).Ok();
    }
    [Fact]
    public async Task ReplyComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: BlogsTestData.CommentTestPostId,
            CreatedBy:"",
            Content:"test"
        );
        comment = await factory.CommentsApiClient.Add(comment).Ok();
        var reply = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: BlogsTestData.CommentTestPostId,
            CreatedBy:"",
            Content:"test"
        );
            
        await factory.CommentsApiClient.Reply(comment.Id, reply).Ok();
        var count = await QueryReplyCount(comment.Id);
        Assert.True(count > 0);
    }
    private async Task<long> QueryReplyCount(string commentId)
    {
        await """
              query commentReplies($source:String){
                commentList(parentSet:[$source],sort:id){
                  id,
                  content,
                }
              }
              """.GraphQlQuery<JsonElement>(factory.QueryApi).Ok();
        var args = new Dictionary<string, StringValues>
        {
            {"source", commentId}
        }; 
        var items = await factory.QueryApi.List("commentReplies",args).Ok();
        return items.Length;

    }
    private async Task<int> QueryCommentsCount()
    {
        await factory.EngagementsApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.CommentTestPostId).Ok();
        await $$"""
                query {{_queryName}}{
                   {{TestEntityNames.TestPost.Camelize()}}(idSet:{{BlogsTestData.CommentTestPostId}})
                   {
                        id,  
                        comments(sort:createdAtDesc){
                            id,
                            content,
                        }
                   }
                }
                """.GraphQlQuery<JsonElement>(factory.QueryApi).Ok();
        var item = await factory.QueryApi.Single(_queryName).Ok();
        return item.GetProperty("comments").GetArrayLength();
    }
    
}