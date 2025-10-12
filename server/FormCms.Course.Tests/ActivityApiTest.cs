using System.Text.Json;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class ActivityApiTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();
    private readonly string _queryName = "qry_query_" + Util.UniqStr();

    [Fact]
    public async Task ActivityCountNotEmpty()
    {
        await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId).Ok();
        var counts = await factory.ActivityApi.ActivityCounts().Ok();
        Assert.True(counts.Length > 0);
    }
    
    [Fact]
    public async Task VisitCountAndPageCount()
    {
        //create home page
        var schema = new Schema("home", SchemaType.Page, new Settings(
            Page: new Page("home", "",null, "home", "", "", "")
        ));
        await factory.SchemaApi.Save(schema);
        
        //authed visit
        await factory.ActivityApi.Visit(factory.GetHttpClient().BaseAddress + "/home");
        var authedCount = await factory.ActivityApi.VisitCounts(true).Ok();
        Assert.True(authedCount.Length > 0);
        await factory.AuthApi.Logout();
        
        //anonymous visit
        await factory.ActivityApi.Visit(factory.GetHttpClient().BaseAddress + "/home");
        await factory.AuthApi.EnsureSaLogin().Ok();
        var anonymouseCount = await factory.ActivityApi.VisitCounts(false).Ok();
        Assert.True(anonymouseCount.Length > 0);

        //page count
        var pageCount = await factory.ActivityApi.PageCounts().Ok();
        Assert.True(pageCount.Length > 0);
    }

    [Fact]
    public async Task BatchGetActivityStatus()
    {
        await factory.ActivityApi.Toggle(TestEntityNames.TestPost.Camelize(), BlogsTestData.LikeTestPostId, "like", true).Ok();
        var liked = await factory.ActivityApi.BatchGetActivityStatus(TestEntityNames.TestPost.Camelize(), "like",[BlogsTestData.LikeTestPostId]).Ok();
        Assert.True(liked.Length > 0);
    }
    
    [Fact]
    public async Task ListHistoryAndDelete()
    {
        await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId).Ok();
        var res = await factory.ActivityApi.List("view", "sort[id]=-1").Ok();
        Assert.True(res.TotalRecords >= 1);
        var totalRecords = res.TotalRecords;
        var item = res.Items[0];

        var id = item.GetLong("id");

        await factory.ActivityApi.Delete(id).Ok();
        res = await factory.ActivityApi.List("view", "").Ok();
        Assert.True(res.TotalRecords < totalRecords);
    }

    [Fact]
    public async Task QueryWithViewCount()
    {
        await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId).Ok();
        await $$"""
                query {{_queryName}}{
                   {{TestEntityNames.TestPost.Camelize()}}(idSet:{{BlogsTestData.ActivityTestPostId}})
                   {
                     id, viewCount 
                   }
                }
                """.GraphQlQuery<JsonElement>(factory.QueryApi).Ok();
        var item = await factory.QueryApi.List(_queryName).Ok();
        Assert.True(item.First().GetProperty("viewCount").GetInt64() > 0);
    }

    
    [Fact]
    private async Task ViewShareLike()
    {
        //get
        var rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId).Ok();

        //view count increase automatically
        var viewElement = rootElement.GetProperty("view");
        Assert.True(viewElement.GetProperty("active").GetBoolean());
        Assert.True(viewElement.GetProperty("count").GetInt64() > 0);

        //like count should be 0
        var likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());

        //record share 
        var count = await factory.ActivityApi.Record(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId, "share").Ok();
        Assert.True(count>0);
        await factory.ActivityApi.Record(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId, "share").Ok();
        rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId).Ok();
        var shareElement = rootElement.GetProperty("share");
        Assert.True(shareElement.GetProperty("active").GetBoolean());
        Assert.True(2 <= shareElement.GetProperty("count").GetInt64());

        //toggle like
        count = await factory.ActivityApi.Toggle(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId, "like", true).Ok();
        Assert.Equal(1, count);
        rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.True(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(1, likeElement.GetProperty("count").GetInt64());

        //cancel like
        count = await factory.ActivityApi.Toggle(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId, "like", false).Ok();
        Assert.Equal(0, count);
        rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), BlogsTestData.ActivityTestPostId).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());
    }
}