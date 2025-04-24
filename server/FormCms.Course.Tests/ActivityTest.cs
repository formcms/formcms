using FormCMS.Activities.ApiClient;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Course.Tests;

public class ActivityTest
{

    private readonly ActivityApiClient _activityApiClient;
    private const long RecordId = 21;

    public ActivityTest()
    {
        Util.SetTestConnectionString();
        var webAppClient = new WebAppClient<Program>();
        Util.LoginAndInitTestData(webAppClient.GetHttpClient()).GetAwaiter().GetResult();
        _activityApiClient = new ActivityApiClient(webAppClient.GetHttpClient());
    }

    public async Task EnsureActivity()
    {
        await TestListHistoryAndDelete();
        await ViewShareLike();
    }

    //have to disable activity cache
    private async Task TestListHistoryAndDelete()
    {
        await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        var res = await _activityApiClient.List("view", "sort[id]=-1").Ok();
        Assert.True(res.TotalRecords >= 1);
        var totalRecords = res.TotalRecords;
        var item = res.Items[0];

        var id = item.GetLong("id");

        await _activityApiClient.Delete(id).Ok();
        res = await _activityApiClient.List("view", "").Ok();
        Assert.True(res.TotalRecords < totalRecords);
    }

    private async Task ViewShareLike()
    {
        //get
        var rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();

        //view count increase automatically
        var viewElement = rootElement.GetProperty("view");
        Assert.True(viewElement.GetProperty("active").GetBoolean());
        Assert.True(viewElement.GetProperty("count").GetInt64() > 0);

        //like count should be 0
        var likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());

        //record share 
        var count = await _activityApiClient.Record(TestEntityNames.TestPost.Camelize(), RecordId, "share").Ok();
        Assert.Equal(1, count);
        await _activityApiClient.Record(TestEntityNames.TestPost.Camelize(), RecordId, "share").Ok();
        rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        var shareElement = rootElement.GetProperty("share");
        Assert.True(shareElement.GetProperty("active").GetBoolean());
        Assert.Equal(2, shareElement.GetProperty("count").GetInt64());

        //toggle like
        count = await _activityApiClient.Toggle(TestEntityNames.TestPost.Camelize(), RecordId, "like", true).Ok();
        Assert.Equal(1, count);
        rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.True(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(1, likeElement.GetProperty("count").GetInt64());

        //cancel like
        count = await _activityApiClient.Toggle(TestEntityNames.TestPost.Camelize(), RecordId, "like", false).Ok();
        Assert.Equal(0, count);
        rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());
    }
}