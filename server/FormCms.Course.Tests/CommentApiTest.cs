using System.Text.Json;
using FormCMS.Comments.Models;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using Microsoft.Extensions.Primitives;
using NJsonSchema.Annotations;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class CommentApiTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();
    private const long RecordId = 31;
    private readonly string _queryName = "qry_query_" + Ulid.NewUlid();

    [Fact]
    public async Task AddComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: RecordId,
            CreatedBy:"",
            Content:"test"
            );
        await factory.CommentsApi.Add(comment).Ok();
        var count = await QueryCommentsCount();
        Assert.True(count > 0);
    }
    
  
    
    [Fact]
    public async Task DeleteComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: RecordId,
            CreatedBy:"",
            Content:"test"
        );
        comment = await factory.CommentsApi.Add(comment).Ok();
        var count = await QueryCommentsCount();
        await factory.CommentsApi.Delete(comment.Id).Ok();
        var countAfter = await QueryCommentsCount();
        Assert.NotEqual(count, countAfter);
    }
    
    [Fact]
    public async Task UpdateComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: RecordId,
            CreatedBy:"",
            Content:"test"
        );
        comment = await factory.CommentsApi.Add(comment).Ok();
        await factory.CommentsApi.Update(comment).Ok();
    }
    [Fact]
    public async Task ReplyComment()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: RecordId,
            CreatedBy:"",
            Content:"test"
        );
        comment = await factory.CommentsApi.Add(comment).Ok();
        var reply = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: RecordId,
            CreatedBy:"",
            Content:"test"
        );
            
        await factory.CommentsApi.Reply(comment.Id, reply).Ok();
        var count = await QueryReplyCount(comment.Id);
        Assert.True(count > 0);
    }
    private async Task<long> QueryReplyCount(long commentId)
    {
        await """
              query commentReplies($source:Int){
                commentList(parentSet:[$source],sort:id){
                  id,
                  content,
                }
              }
              """.GraphQlQuery<JsonElement>(factory.QueryApi).Ok();
        var args = new Dictionary<string, StringValues>
        {
            {"source", commentId.ToString()}
        }; 
        var items = await factory.QueryApi.List("commentReplies",args).Ok();
        return items.Length;

    }
    private async Task<int> QueryCommentsCount()
    {
        await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        await $$"""
                query {{_queryName}}{
                   {{TestEntityNames.TestPost.Camelize()}}(idSet:{{RecordId}})
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