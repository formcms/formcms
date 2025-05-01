using System.Text.Json;
using FormCMS.AuditLogging.ApiClient;
using FormCMS.AuditLogging.Models;
using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using NUlid;
using Xunit.Abstractions;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class AuditLogApiTest(AppFactory factory)
{
    private readonly string _post = "audit_post_" + Ulid.NewUlid();
    private bool _ = factory.LoginAndInitTestData();

    [Fact]
    public async Task EnsureAuditLogEntityExists()
    {
        var entity = await factory.AuditLogApi.AuditEntity().Ok();
        Assert.NotNull(entity);
    }

    [Fact]
    public async Task EnsureOptionAddUpdateDelete()
    {
        var res = await factory.AuditLogApi.List("sort[id]=-1").Ok();
        var logId = res.Items.Length > 0 ? res.Items[0].GetLong("id"):0;
        
        
        await factory.SchemaApi.EnsureSimpleEntity(_post,"name",false).Ok();
        var item = await factory.EntityApi.Insert(_post,"name","test").Ok();
        var log = await factory.AuditLogApi.Single(++logId).Ok();
        Assert.Equal(ActionType.Create,log.Action);

        item = await factory.EntityApi.Single(_post, item.GetProperty("id").GetInt32()).Ok();
        await factory.EntityApi.Update(_post, item.ToDictionary());
        log = await factory.AuditLogApi.Single(++logId).Ok();
        Assert.Equal(ActionType.Update,log.Action);

        item = await  factory.EntityApi.Single(_post, item.GetProperty("id").GetInt32()).Ok();
        await  factory.EntityApi.Delete(_post, item.ToDictionary());
        log = await factory.AuditLogApi.Single(++logId).Ok();
        Assert.Equal(ActionType.Delete,log.Action);
    }
    
    [Fact]
    public async Task EnsureAuditLogAddListSingle()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post,"name",false).Ok();
        await  factory.EntityApi.Insert(_post,"name","test").Ok();
        var res = await factory.AuditLogApi.List("sort[id]=-1").Ok();
        var item = res.Items[0];
        var ele = item[nameof(AuditLog.EntityName).Camelize()];
        Assert.True(ele is JsonElement entityElement && entityElement.GetString() == _post);
        
        var id = (JsonElement)item[nameof(AuditLog.Id).Camelize()];
        var log = await factory.AuditLogApi.Single(id.GetInt32()).Ok();
        var name = (JsonElement)log.Payload["name"];
        Assert.Equal("test", name.GetString());
    }
    
    
}