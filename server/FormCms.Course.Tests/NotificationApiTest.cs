using FormCMS.Comments.Models;
using FormCMS.CoreKit.Test;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Course.Tests;

[Collection("API")]
public class NotificationApiTest(AppFactory factory)
{
    private bool _ = factory.LoginAndInitTestData();
    private const long RecordId = 31;

    [Fact]
    public async Task ListNotification()
    {
        var comment = new Comment(
            EntityName:TestEntityNames.TestPost.Camelize(),
            RecordId: RecordId,
            CreatedBy:"",
            Content:"test"
        );
        await factory.CommentsApiClient.Add(comment).Ok();
        var count =await factory.NotifyApiClient.UnreadCount().Ok();
        Assert.True(count > 0);

        var res = await factory.NotifyApiClient.List().Ok();
        Assert.True(res.TotalRecords > 0);
        
        
        count =await factory.NotifyApiClient.UnreadCount().Ok();
        Assert.True(count == 0);
    }
}