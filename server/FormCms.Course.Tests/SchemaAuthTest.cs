using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Mvc.Testing;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class SchemaAuthTest
{
    private readonly string _saPost = "sa_post_" + Ulid.NewUlid();
    private readonly string _adminPost = "sa_post_" + Ulid.NewUlid();
    private readonly string _email = $"sa_user_{Ulid.NewUlid()}@cms.com";
    private const string Pwd = "Admin1!";
    private AppFactory Factory { get; }

    public SchemaAuthTest(AppFactory factory)
    {
        Factory = factory;
        factory.AuthApi.RegisterAndLogin(_email,Pwd).GetAwaiter().GetResult();
        EnsureEntityExists().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AnonymousCanNotGetSchema()
    {
        Assert.True((await Factory.SchemaApi.All(null)).IsFailed);
        Assert.True((await Factory.SchemaApi.GetTopMenuBar()).IsFailed);
        Assert.True((await Factory.SchemaApi.GetLoadedEntity(_saPost)).IsFailed);
    }

    //all users can view top menu bar and XEntity
    [Fact]
    public Task UserCanViewTopMenuBarAndXEntity() =>  Factory.AuthApi.Sudo(_email, Pwd, async () =>
    {
        Assert.True((await Factory.SchemaApi.GetTopMenuBar()).IsFailed);
        Assert.True((await Factory.SchemaApi.GetLoadedEntity(_saPost)).IsFailed);
        //but ordinary user cannot view all schemas
        Assert.True((await Factory.SchemaApi.All(null)).IsFailed);
    });

    [Fact]
    public Task SaSchemaAuth() => Factory.AuthApi.SaDo(async () =>
    {
        await Factory.SchemaApi.GetTopMenuBar().Ok();
        await Factory.SchemaApi.GetLoadedEntity(_saPost).Ok();
        var schemas = await Factory.SchemaApi.All(null).Ok();
        await Factory.SchemaApi.Single(schemas.First().Id).Ok();
        
        var adminEntity = schemas.FirstOrDefault(x=>x.Name == _adminPost)!;
        await Factory.SchemaApi.SaveEntityDefine(adminEntity).Ok();
        
        var saEntity = schemas.FirstOrDefault(x=>x.Name == _saPost)!;
        await Factory.SchemaApi.SaveEntityDefine(saEntity).Ok();

    });

    [Fact]
    public Task AdminSchemaAuth() =>  Factory.AuthApi.AdminDo(async () =>
    {
        await Factory.SchemaApi.GetTopMenuBar().Ok();
        await Factory.SchemaApi.GetLoadedEntity(_saPost).Ok();
        
        var schemas = await Factory.SchemaApi.All(null).Ok();
        await Factory.SchemaApi.Single(schemas.First().Id).Ok();
        
        var adminEntity = schemas.FirstOrDefault(x=>x.Name == _adminPost)!;
        await Factory.SchemaApi.SaveEntityDefine(adminEntity).Ok();
        
        //admin cannot save sa's schema
        var saEntity = schemas.FirstOrDefault(x=>x.Name == _saPost)!;
        var res = await Factory.SchemaApi.SaveEntityDefine(saEntity);
        Assert.True(res.IsFailed);
    });
    
    private async Task EnsureEntityExists()
    {
        await Factory.AuthApi.SaDo(async () =>
        {
            await Factory.SchemaApi.EnsureSimpleEntity(_saPost, "name",false).Ok();
        });
        
        await Factory.AuthApi.AdminDo(async () =>
        {
            await Factory.SchemaApi.EnsureSimpleEntity(_adminPost, "name", false).Ok();
        });
    }
}

