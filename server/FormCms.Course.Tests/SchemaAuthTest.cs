using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;

public class SchemaAuthTest
{
    private readonly SchemaApiClient _schema;
    private readonly AuthApiClient _auth;
    private readonly string _saPost = "sa_post_" + Ulid.NewUlid();
    private readonly string _adminPost = "sa_post_" + Ulid.NewUlid();
    private readonly string _email = $"sa_user_{Ulid.NewUlid()}@cms.com";
    private const string Pwd = "Admin1!";

    public SchemaAuthTest()
    {
        Util.SetTestConnectionString();
        var webAppClient = new WebAppClient<Program>();
        _auth = new AuthApiClient(webAppClient.GetHttpClient());
        _schema = new SchemaApiClient(webAppClient.GetHttpClient());
        EnsureEntityExists().GetAwaiter().GetResult();
        _auth.Register(_email, Pwd).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AnonymousCanNotGetSchema()
    {
        Assert.True((await _schema.All(null)).IsFailed);
        Assert.True((await _schema.GetTopMenuBar()).IsFailed);
        Assert.True((await _schema.GetLoadedEntity(_saPost)).IsFailed);
    }

    //all users can view top menu bar and XEntity 
    [Fact]
    public Task UserCanViewTopMenuBarAndXEntity() => _auth.Sudo(_email, Pwd, async () =>
    {
        await _schema.GetTopMenuBar().Ok();
        await _schema.GetLoadedEntity(_saPost).Ok();
        //but ordinary user cannot view all schemas
        Assert.True((await _schema.All(null)).IsFailed);
    });

    [Fact]
    public Task SaSchemaAuth() => _auth.SaDo(async () =>
    {
        await _schema.GetTopMenuBar().Ok();
        await _schema.GetLoadedEntity(_saPost).Ok();
        var schemas = await _schema.All(null).Ok();
        await _schema.Single(schemas.First().Id).Ok();
        
        var adminEntity = schemas.FirstOrDefault(x=>x.Name == _adminPost)!;
        await _schema.SaveEntityDefine(adminEntity).Ok();
        
        var saEntity = schemas.FirstOrDefault(x=>x.Name == _saPost)!;
        await _schema.SaveEntityDefine(saEntity).Ok();

    });

    [Fact]
    public Task AdminSchemaAuth() => _auth.AdminDo(async () =>
    {
        await _schema.GetTopMenuBar().Ok();
        await _schema.GetLoadedEntity(_saPost).Ok();
        
        var schemas = await _schema.All(null).Ok();
        await _schema.Single(schemas.First().Id).Ok();
        
        var adminEntity = schemas.FirstOrDefault(x=>x.Name == _adminPost)!;
        await _schema.SaveEntityDefine(adminEntity).Ok();
        
        //admin cannot save sa's schema
        var saEntity = schemas.FirstOrDefault(x=>x.Name == _saPost)!;
        var res = await _schema.SaveEntityDefine(saEntity);
        Assert.True(res.IsFailed);
    });
    
    private async Task EnsureEntityExists()
    {
        await _auth.SaDo(async () =>
        {
            await _schema.EnsureSimpleEntity(_saPost, "name",false).Ok();
        });
        
        await _auth.AdminDo(async () =>
        {
            await _schema.EnsureSimpleEntity(_adminPost, "name", false).Ok();
        });
    }
}

