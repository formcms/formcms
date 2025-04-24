using System.Text.Json;
using FormCMS.Activities.ApiClient;
using FormCMS.Activities.Models;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Authentication;
using NUlid;

namespace FormCMS.Course.Tests;

public class BookmarkTest
{
    private readonly ActivityApiClient _activityApiClient;
    private readonly BookmarkApiClient _bookmarkApiClient;
    private const  long  RecordId = 22;

    public BookmarkTest()
    {
        Util.SetTestConnectionString();
        WebAppClient<Program> webAppClient = new();
        Util.LoginAndInitTestData(webAppClient.GetHttpClient()).GetAwaiter().GetResult();
        _bookmarkApiClient = new BookmarkApiClient(webAppClient.GetHttpClient());
        _activityApiClient = new ActivityApiClient(webAppClient.GetHttpClient());

    }

    public async Task EsnureBookmark()
    {
        await ListBookmarkByFolderIdAndDelete();
        await FolderUpdateAndDelete();
        await SaveToDefaultFolder();
        await SaveAndCreateFolderOnTheFlight();
    }

    private async Task ListBookmarkByFolderIdAndDelete()
    {
        var name = Ulid.NewUlid().ToString();
        await _bookmarkApiClient.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, name, []).Ok();
        var folders = await _bookmarkApiClient.AllFolders().Ok();
        var folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);

        //delete bookmark
        var response = await _bookmarkApiClient.ListBookmarks(folder.GetProperty("id").GetInt64()).Ok();
        var id = response.Items[0].GetLong("id");
        await _bookmarkApiClient.DeleteBookmark(id).Ok();
        response = await _bookmarkApiClient.ListBookmarks(folder.GetProperty("id").GetInt64()).Ok();
        Assert.Equal(0, response.TotalRecords);

        //delete folder
        await _bookmarkApiClient.DeleteFolder(folder.GetProperty("id").GetInt64()).Ok();
        folders = await _bookmarkApiClient.AllFolders().Ok();
        folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);
        Assert.Equal(JsonValueKind.Undefined,folder.ValueKind);
    }

    private async Task FolderUpdateAndDelete()
    {
        //add
        var name = Ulid.NewUlid().ToString();
        await _bookmarkApiClient.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, name, []).Ok();
        var folders = await _bookmarkApiClient.AllFolders().Ok();
        var folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);
        Assert.NotEqual(JsonValueKind.Undefined,folder.ValueKind);
    
        //update
        var bookmarkFolder = new BookmarkFolder("", folder.GetProperty("name").GetString()!, "test");
        await _bookmarkApiClient.UpdateFolder(folder.GetProperty("id").GetInt64(), bookmarkFolder).Ok();
        folders = await _bookmarkApiClient.AllFolders().Ok();
        folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);
        Assert.Equal("test", folder.GetProperty("description").GetString());
    
    }

    private async Task SaveToDefaultFolder()
    {
        var res =await _bookmarkApiClient.ListBookmarks(0).Ok();
        foreach (var item in res.Items)
        {
            await _bookmarkApiClient.DeleteBookmark(item.GetLong("id")).Ok();
        }
        //save to default folder
        await _bookmarkApiClient.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, "", [0]).Ok();
        var folders = await _bookmarkApiClient.FolderWithRecordStatus(TestEntityNames.TestPost.Camelize(), 1).Ok();
        Assert.True(folders.First(x => x.Id == 0).Selected);
    }

    private async Task SaveAndCreateFolderOnTheFlight()
    {
        var name = Ulid.NewUlid().ToString();
        await _bookmarkApiClient.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, name, [0]).Ok();
        var folders = await _bookmarkApiClient.FolderWithRecordStatus(TestEntityNames.TestPost.Camelize(), 1).Ok();
        Assert.NotNull(folders.FirstOrDefault(x => x.Name == name));
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