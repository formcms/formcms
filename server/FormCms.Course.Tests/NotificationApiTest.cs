using FormCMS.Comments.Models;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Course.Tests;

[Collection("API")]
public class NotificationApiTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();

    [Fact]
    public async Task ListNotification()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: BlogsTestData.NotificationTestPostId,
            CreatedBy:"",
            Content:"test"
        );
        await factory.CommentsApiClient.Add(comment).Ok();
        Thread.Sleep(TimeSpan.FromSeconds(1));
        var count =await factory.NotifyApiClient.UnreadCount().Ok();
        Assert.True(count > 0);

        var res = await factory.NotifyApiClient.List().Ok();
        Assert.True(res.TotalRecords > 0);
        
        
        count =await factory.NotifyApiClient.UnreadCount().Ok();
        Assert.Equal(0, count);
    }
}