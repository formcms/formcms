using FormCMS.Activities.ApiClient;
using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;

public class ActivityTest
{
    private const string Name = "name";
    private readonly string _post = "activity_post_" + Ulid.NewUlid();

    private readonly EntityApiClient _entityApiClient;
    private readonly SchemaApiClient _schemaApiClient;
    private readonly ActivityApiClient _activityApiClient;

    public ActivityTest()
    {
        Util.SetTestConnectionString();
        
        WebAppClient<Program> webAppClient = new();
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _activityApiClient = new ActivityApiClient(webAppClient.GetHttpClient());
        new AuthApiClient(webAppClient.GetHttpClient()).EnsureSaLogin().Ok().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ViewShareLike()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name, false).Ok();
        await _entityApiClient.Insert(_post, Name, "name1").Ok();

        //get
        var rootElement = await _activityApiClient.Get(_post, 1).Ok();

        //view count increase automatically
        var viewElement = rootElement.GetProperty("view");
        Assert.True(viewElement.GetProperty("active").GetBoolean());
        Assert.Equal(1, viewElement.GetProperty("count").GetInt64());

        //like count should be 0
        var likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());

        //record share 
        var count = await _activityApiClient.Record(_post, 1, "share").Ok();
        Assert.Equal(1, count);
        await _activityApiClient.Record(_post, 1, "share").Ok();
        rootElement = await _activityApiClient.Get(_post, 1).Ok();
        var shareElement = rootElement.GetProperty("share");
        Assert.True(shareElement.GetProperty("active").GetBoolean());
        Assert.Equal(2, shareElement.GetProperty("count").GetInt64());



        //toggle like
        count = await _activityApiClient.Toggle(_post, 1, "like", true).Ok();
        Assert.Equal(1, count);
        rootElement = await _activityApiClient.Get(_post, 1).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.True(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(1, likeElement.GetProperty("count").GetInt64());

        //cancel like
        count = await _activityApiClient.Toggle(_post, 1, "like", false).Ok();
        Assert.Equal(0, count);
        rootElement = await _activityApiClient.Get(_post, 1).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());
    }
}