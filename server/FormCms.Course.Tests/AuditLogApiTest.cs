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

public class AuditLogApiTest
{
    private readonly SchemaApiClient _schemaApiClient;
    private readonly EntityApiClient _entityApiClient;
    private readonly AuditLogApiClient _auditLogApiClient;

    private readonly string _post = "audit_post_" + Ulid.NewUlid();

    public AuditLogApiTest(ITestOutputHelper testOutputHelper)
    {
        Util.SetTestConnectionString();
        
        var webAppClient = new WebAppClient<Program>();
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _auditLogApiClient = new AuditLogApiClient(webAppClient.GetHttpClient());

        new AuthApiClient(webAppClient.GetHttpClient()).EnsureSaLogin().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task EnsureAuditLogEntityExists()
    {
        var entity = await _auditLogApiClient.AuditEntity().Ok();
        Assert.NotNull(entity);
    }

    [Fact]
    public async Task EnsureOptionAddUpdateDelete()
    {
        var res = await _auditLogApiClient.List("sort[id]=-1").Ok();
        var logId = res.Items.Length > 0 ? res.Items[0].GetLong("id"):0;
        
        
        await _schemaApiClient.EnsureSimpleEntity(_post,"name",false).Ok();
        var item = await _entityApiClient.Insert(_post,"name","test").Ok();
        var log = await _auditLogApiClient.Single(++logId).Ok();
        Assert.Equal(ActionType.Create,log.Action);

        item = await _entityApiClient.Single(_post, item.GetProperty("id").GetInt32()).Ok();
        await _entityApiClient.Update(_post, item.ToDictionary());
        log = await _auditLogApiClient.Single(++logId).Ok();
        Assert.Equal(ActionType.Update,log.Action);

        item = await _entityApiClient.Single(_post, item.GetProperty("id").GetInt32()).Ok();
        await _entityApiClient.Delete(_post, item.ToDictionary());
        log = await _auditLogApiClient.Single(++logId).Ok();
        Assert.Equal(ActionType.Delete,log.Action);
    }
    
    [Fact]
    public async Task EnsureAuditLogAddListSingle()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post,"name",false).Ok();
        await _entityApiClient.Insert(_post,"name","test").Ok();
        var res = await _auditLogApiClient.List("sort[id]=-1").Ok();
        var item = res.Items[0];
        var ele = item[nameof(AuditLog.EntityName).Camelize()];
        Assert.True(ele is JsonElement entityElement && entityElement.GetString() == _post);
        
        var id = (JsonElement)item[nameof(AuditLog.Id).Camelize()];
        var log = await _auditLogApiClient.Single(id.GetInt32()).Ok();
        var name = (JsonElement)log.Payload["name"];
        Assert.Equal("test", name.GetString());
    }
    
    
}