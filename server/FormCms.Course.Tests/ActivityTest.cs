using FormCMS.Activities.ApiClient;
using FormCMS.Activities.Models;
using FormCMS.Auth.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.ApiClient;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using NUlid;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;

public class ActivityTest
{

    private readonly ActivityApiClient _activityApiClient;
    private readonly BookmarkApiClient _bookmarkApiClient;
 
    public ActivityTest()
    {
        // Util.SetTestConnectionString();
        
        WebAppClient<Program> webAppClient = new();
        Util.LoginAndInitTestData(webAppClient.GetHttpClient()).GetAwaiter().GetResult();
        _activityApiClient = new ActivityApiClient(webAppClient.GetHttpClient());
        _bookmarkApiClient = new BookmarkApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async Task CreateFolderAndBookmark()
    {
        //save without folder
        await _bookmarkApiClient.AddBookmarks(TestEntityNames.TestPost.Camelize(), 1, "",[0]).Ok();
        var folders = await _bookmarkApiClient.FolderWithRecordStatus(TestEntityNames.TestPost.Camelize(), 1).Ok();
        Assert.True(folders.First(x=>x.Id == 0).Selected);

        var name =  Ulid.NewUlid().ToString();
        await _bookmarkApiClient.AddBookmarks(TestEntityNames.TestPost.Camelize(), 1, name,[0]).Ok();
        folders = await _bookmarkApiClient.FolderWithRecordStatus(TestEntityNames.TestPost.Camelize(), 1).Ok();
        Assert.NotNull(folders.FirstOrDefault(x=>x.Name == name));
    }
    
    [Fact]
    public async Task TestHistory()
    {
        await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), 1).Ok();
        //now should have one history
        var res = await _activityApiClient.List("view","").Ok();
        Assert.True(res.TotalRecords >=1);
    }

    [Fact]
    public async Task ViewShareLike()
    {
        //get
        var rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), 1).Ok();

        //view count increase automatically
        var viewElement = rootElement.GetProperty("view");
        Assert.True(viewElement.GetProperty("active").GetBoolean());
        Assert.Equal(1, viewElement.GetProperty("count").GetInt64());

        //like count should be 0
        var likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());

        //record share 
        var count = await _activityApiClient.Record(TestEntityNames.TestPost.Camelize(), 1, "share").Ok();
        Assert.Equal(1, count);
        await _activityApiClient.Record(TestEntityNames.TestPost.Camelize(), 1, "share").Ok();
        rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), 1).Ok();
        var shareElement = rootElement.GetProperty("share");
        Assert.True(shareElement.GetProperty("active").GetBoolean());
        Assert.Equal(2, shareElement.GetProperty("count").GetInt64());

        //toggle like
        count = await _activityApiClient.Toggle(TestEntityNames.TestPost.Camelize(), 1, "like", true).Ok();
        Assert.Equal(1, count);
        rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), 1).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.True(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(1, likeElement.GetProperty("count").GetInt64());

        //cancel like
        count = await _activityApiClient.Toggle(TestEntityNames.TestPost.Camelize(), 1, "like", false).Ok();
        Assert.Equal(0, count);
        rootElement = await _activityApiClient.Get(TestEntityNames.TestPost.Camelize(), 1).Ok();
        likeElement = rootElement.GetProperty("like");
        Assert.False(likeElement.GetProperty("active").GetBoolean());
        Assert.Equal(0, likeElement.GetProperty("count").GetInt64());
    }
}