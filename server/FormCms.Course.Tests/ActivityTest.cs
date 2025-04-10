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
    public async Task AddActivity()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post,Name,false).Ok();
        await _entityApiClient.Insert(_post, Name, "name1").Ok();
        var count = await _activityApiClient.Toggle(_post,1,"like",true).Ok();
        Assert.Equal(1,count);
        var element = await _activityApiClient.Get(_post, 1).Ok();
        var like = element.GetProperty("like");
        Assert.True(like.GetProperty("active").GetBoolean());
        Assert.Equal(1,like.GetProperty("count").GetInt64());
    }
}