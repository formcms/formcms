using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class ActivityApiTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();
    private const long RecordId = 21;

    [Fact]
    public async Task ActivityCountNotEmpty()
    {
        await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
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
        
        //anonymous visit
        await factory.ActivityApi.Visit(factory.GetHttpClient().BaseAddress + "/home");
        var authedCount = await factory.ActivityApi.VisitCounts(true).Ok();
        Assert.True(authedCount.Length > 0);
        
        //authed visit
        await factory.AuthApi.Logout();
        await factory.ActivityApi.Visit(factory.GetHttpClient().BaseAddress + "/home");
        await factory.AuthApi.EnsureSaLogin().Ok();
        var anonymouseCount = await factory.ActivityApi.VisitCounts(false).Ok();
        Assert.True(anonymouseCount.Length > 0);

        //page count
        var pageCount = await factory.ActivityApi.PageCounts().Ok();
        Assert.True(pageCount.Length > 0);
    }
    
    [Fact]
    public async Task TestListHistoryAndDelete()
    {
        await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
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
    private async Task ViewShareLike()
    {
        //get
        var rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();

        //view count increase automatically
        var viewElement = rootElement.GetProperty("view");
        Assert.True(viewElement.GetProperty("active").GetBoolean());
        Assert.True(viewElement.GetProperty("count").GetInt64() > 0);

        //like count should be 0
        var likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());

        //record share 
        var count = await factory.ActivityApi.Record(TestEntityNames.TestPost.Camelize(), RecordId, "share").Ok();
        Assert.True(count>0);
        await factory.ActivityApi.Record(TestEntityNames.TestPost.Camelize(), RecordId, "share").Ok();
        rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        var shareElement = rootElement.GetProperty("share");
        Assert.True(shareElement.GetProperty("active").GetBoolean());
        Assert.Equal(2, shareElement.GetProperty("count").GetInt64());

        //toggle like
        count = await factory.ActivityApi.Toggle(TestEntityNames.TestPost.Camelize(), RecordId, "like", true).Ok();
        Assert.Equal(1, count);
        rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.True(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(1, likeElement.GetProperty("count").GetInt64());

        //cancel like
        count = await factory.ActivityApi.Toggle(TestEntityNames.TestPost.Camelize(), RecordId, "like", false).Ok();
        Assert.Equal(0, count);
        rootElement = await factory.ActivityApi.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());
    }
}