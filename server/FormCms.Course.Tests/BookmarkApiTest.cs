using System.Text.Json;
using FormCMS.Activities.ApiClient;
using FormCMS.Activities.Models;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class BookmarkApiTest(AppFactory factory)
{
    private const  long  RecordId = 22;
    private bool _ = factory.LoginAndInitTestData();

    [Fact]
    public async Task ListBookmarkByFolderIdAndDelete()
    {
        var name = Ulid.NewUlid().ToString();
        await factory.BookmarkApi.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, name, []).Ok();
        var folders = await factory.BookmarkApi.AllFolders().Ok();
        var folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);

        //delete bookmark
        var response = await factory.BookmarkApi.ListBookmarks(folder.GetProperty("id").GetInt64()).Ok();
        var id = response.Items[0].GetLong("id");
        await factory.BookmarkApi.DeleteBookmark(id).Ok();
        response = await factory.BookmarkApi.ListBookmarks(folder.GetProperty("id").GetInt64()).Ok();
        Assert.Equal(0, response.TotalRecords);

        //delete folder
        await factory.BookmarkApi.DeleteFolder(folder.GetProperty("id").GetInt64()).Ok();
        folders = await factory.BookmarkApi.AllFolders().Ok();
        folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);
        Assert.Equal(JsonValueKind.Undefined,folder.ValueKind);
    }

    [Fact]
    public async Task FolderUpdateAndDelete()
    {
        //add
        var name = Ulid.NewUlid().ToString();
        await factory.BookmarkApi.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, name, []).Ok();
        var folders = await factory.BookmarkApi.AllFolders().Ok();
        var folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);
        Assert.NotEqual(JsonValueKind.Undefined,folder.ValueKind);
    
        //update
        var bookmarkFolder = new BookmarkFolder("", folder.GetProperty("name").GetString()!, "test");
        await factory.BookmarkApi.UpdateFolder(folder.GetProperty("id").GetInt64(), bookmarkFolder).Ok();
        folders = await factory.BookmarkApi.AllFolders().Ok();
        folder = folders.FirstOrDefault(x => x.GetProperty("name").GetString() == name);
        Assert.Equal("test", folder.GetProperty("description").GetString());
    
    }
    [Fact]
    public async Task SaveToDefaultFolder()
    {
        var res =await factory.BookmarkApi.ListBookmarks(0).Ok();
        foreach (var item in res.Items)
        {
            await factory.BookmarkApi.DeleteBookmark(item.GetLong("id")).Ok();
        }
        //save to default folder
        await factory.BookmarkApi.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, "", [0]).Ok();
        var folders = await factory.BookmarkApi.FolderWithRecordStatus(TestEntityNames.TestPost.Camelize(), RecordId).Ok();
        Assert.True(folders.First(x => x.Id == 0).Selected);
    }

    [Fact]
    public async Task SaveAndCreateFolderOnTheFlight()
    {
        var name = Ulid.NewUlid().ToString();
        await factory.BookmarkApi.AddBookmarks(TestEntityNames.TestPost.Camelize(), RecordId, name, [0]).Ok();
        var folders = await factory.BookmarkApi.FolderWithRecordStatus(TestEntityNames.TestPost.Camelize(), 1).Ok();
        Assert.NotNull(folders.FirstOrDefault(x => x.Name == name));
    }
}