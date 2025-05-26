using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class EntityAuthTest
{
    private readonly string _post = "ea_post_" + Ulid.NewUlid();

    private readonly string _email = $"ea_user_{Ulid.NewUlid()}@cms.com";
    private const string Pwd = "Admin1!";
    private readonly string _role = $"ea_role_{Ulid.NewUlid()}";
    private long _saPostId = 0;
    private const string Name = "name";
    private AppFactory Factory { get; }

    public EntityAuthTest(AppFactory factory)
    {
        Factory = factory;
        Factory.AuthApi.Register(_email.Split('@')[0],_email, Pwd).GetAwaiter().GetResult();
        EnsureEntityExists().GetAwaiter().GetResult();
    }

    [Fact]
    public Task AnonymousUser() => ReadAddFail();
    
    [Fact]
    public Task EmptyUser() =>  Factory.AuthApi.Sudo(_email, Pwd, ReadAddFail);
    
    [Fact]
    public async Task UserRestrictedRead()
    {
        await Factory.AuthApi.SaDo(() => Factory.AccountApi.AssignEntityToUser(_email, restrictedRead: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
    }
    
    [Fact]
    public async Task RoleRestrictedRead()
    {
        await Factory.AuthApi.SaDo(() => Factory.AccountApi.AssignEntityToUserByRole(_email, _role, restrictedRead: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
    }
   
    [Fact]
    public async Task UserFullRead()
    {
        await Factory.AuthApi.SaDo( () => Factory.AccountApi.AssignEntityToUser(_email, fullRead: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherOk);
    } 
    
    [Fact]
    public async Task RoleFullRead()
    {
        await Factory.AuthApi.SaDo( () => Factory.AccountApi.AssignEntityToUserByRole(_email,_role, fullRead: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task UserWithRestrictedWrite()
    {
        await Factory.AuthApi.SaDo( () => Factory.AccountApi.AssignEntityToUser(_email, restrictedWrite: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnOk);
    }

    [Fact]
    public async Task RoleWithRestrictedWrite()
    {
        await Factory.AuthApi.SaDo( () => Factory.AccountApi.AssignEntityToUserByRole(_email,_role, restrictedWrite: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnOk);
    }

    [Fact]
    public async Task UserFullWrite()
    {
        await Factory.AuthApi.SaDo(() => Factory.AccountApi.AssignEntityToUser(_email, fullWrite: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOtherOk);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task RoleFullWrite()
    {
        await Factory.AuthApi.SaDo(() => Factory.AccountApi.AssignEntityToUser(_email, fullWrite: [_post]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOtherOk);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public Task SaUser() => Factory.AuthApi.SaDo(async () =>
    {
        await ReadOwnOk();
        await WriteOwnOk();
        await WriteOtherOk();
        await ReadOtherOk();
    });
    private async Task ReadAddFail()
    {
        var res = await Factory.EntityApi.Insert(_post, Name, "name1");
        Assert.True(res.IsFailed);

        var singleRes = await Factory.EntityApi.Single(_post,_saPostId);
        Assert.True(singleRes.IsFailed);

        var listRes = await Factory.EntityApi.List(_post, 0, 100);
        Assert.True(listRes.IsFailed);
    }
    
    private async Task ReadOwnOk()
    {
        await Factory.EntityApi.List(_post,0,100).Ok();
    }
    
    private async Task ReadOtherOk() 
    {
        await Factory.EntityApi.Single(_post,_saPostId).Ok();
        var list = await Factory.EntityApi.List(_post, 0,100).Ok();
        Assert.True(list.Items.Length > 0);
        Assert.True(list.TotalRecords > 0);
    }
    
    private async Task ReadOtherFail()
    {
        Assert.True((await Factory.EntityApi.Single(_post, _saPostId)).IsFailed);
        
        var list = await Factory.EntityApi.List(_post, 0,100).Ok();
        Assert.Empty(list.Items);
        Assert.Equal(0, list.TotalRecords);
    }
    
    private async Task WriteOwnFail()
    {
        var res = await Factory.EntityApi.Insert(_post, Name, "name1");
        Assert.True(res.IsFailed);
    }
    
    private async Task WriteOwnOk()
    {
        var res = await Factory.EntityApi.Insert(_post, Name,"test").Ok();
        res = await  Factory.EntityApi.Single(_post, res.GetProperty("id").GetInt64()).Ok();
        await Factory.EntityApi.Update(_post, res.ToDictionary()).Ok();
        res = await  Factory.EntityApi.Single(_post, res.GetProperty("id").GetInt64()).Ok();
        await Factory.EntityApi.Delete(_post, res.ToDictionary()).Ok();
    } 
   
    private async Task WriteOtherOk() 
    {
        var res = await Factory.EntityApi.Single(_post, _saPostId).Ok();
        await Factory.EntityApi.Update(_post, res.ToDictionary()).Ok();
    }
    
    private async Task EnsureEntityExists()
    {
        await Factory.AuthApi.SaDo(async () =>
        {
            await Factory.SchemaApi.EnsureSimpleEntity(_post, Name,false).Ok();
            var res=await Factory.EntityApi.Insert(_post, Name, "name1").Ok();
            _saPostId = res.GetProperty("id").GetInt64();
        });
    }

}