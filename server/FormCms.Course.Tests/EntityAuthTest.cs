using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;

public class EntityAuthTest
{
    private readonly string _post = "ea_post_" + Ulid.NewUlid();

    private readonly AuthApiClient _auth;
    private readonly AccountApiClient _account;
    private readonly SchemaApiClient _schemaApiClient;
    private readonly EntityApiClient _entity;
    private readonly string _email = $"ea_user_{Ulid.NewUlid()}@cms.com";
    private const string Pwd = "Admin1!";
    private readonly string _role = $"ea_role_{Ulid.NewUlid()}";
    private long _saPostId = 0;
    private const string Name = "name";

    public EntityAuthTest()
    {
        Util.SetTestConnectionString();
        var webAppClient = new WebAppClient<Program>();
        _auth = new AuthApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _account = new AccountApiClient(webAppClient.GetHttpClient());
        _entity = new EntityApiClient(webAppClient.GetHttpClient());
        
        _auth.Register(_email, Pwd).GetAwaiter().GetResult();
        EnsureEntityExists().GetAwaiter().GetResult();
    }

    [Fact]
    public Task AnonymousUser() => ReadAddFail();
    
    [Fact]
    public Task EmptyUser() =>  _auth.Sudo(_email, Pwd, ReadAddFail);
    
    [Fact]
    public async Task UserRestrictedRead()
    {
        await _auth.SaDo(() => _account.AssignEntityToUser(_email, restrictedRead: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
    }
    
    [Fact]
    public async Task RoleRestrictedRead()
    {
        await _auth.SaDo(() => _account.AssignEntityToUserByRole(_email, _role, restrictedRead: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
    }
   
    [Fact]
    public async Task UserFullRead()
    {
        await _auth.SaDo( () => _account.AssignEntityToUser(_email, fullRead: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherOk);
    } 
    
    [Fact]
    public async Task RoleFullRead()
    {
        await _auth.SaDo( () => _account.AssignEntityToUserByRole(_email,_role, fullRead: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task UserWithRestrictedWrite()
    {
        await _auth.SaDo( () => _account.AssignEntityToUser(_email, restrictedWrite: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
        await _auth.Sudo(_email, Pwd, WriteOwnOk);
    }

    [Fact]
    public async Task RoleWithRestrictedWrite()
    {
        await _auth.SaDo( () => _account.AssignEntityToUserByRole(_email,_role, restrictedWrite: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
        await _auth.Sudo(_email, Pwd, WriteOwnOk);
    }

    [Fact]
    public async Task UserFullWrite()
    {
        await _auth.SaDo(() => _account.AssignEntityToUser(_email, fullWrite: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOtherOk);
        await _auth.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task RoleFullWrite()
    {
        await _auth.SaDo(() => _account.AssignEntityToUser(_email, fullWrite: [_post]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOtherOk);
        await _auth.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public Task SaUser() => _auth.SaDo(async () =>
    {
        await ReadOwnOk();
        await WriteOwnOk();
        await WriteOtherOk();
        await ReadOtherOk();
    });
    private async Task ReadAddFail()
    {
        var res = await _entity.Insert(_post, Name, "name1");
        Assert.True(res.IsFailed);

        var singleRes = await _entity.Single(_post,_saPostId);
        Assert.True(singleRes.IsFailed);

        var listRes = await _entity.List(_post, 0, 100);
        Assert.True(listRes.IsFailed);
    }
    
    private async Task ReadOwnOk()
    {
        await _entity.List(_post,0,100).Ok();
    }
    
    private async Task ReadOtherOk() 
    {
        await _entity.Single(_post,_saPostId).Ok();
        var list = await _entity.List(_post, 0,100).Ok();
        Assert.True(list.Items.Length > 0);
        Assert.True(list.TotalRecords > 0);
    }
    
    private async Task ReadOtherFail()
    {
        Assert.True((await _entity.Single(_post, _saPostId)).IsFailed);
        
        var list = await _entity.List(_post, 0,100).Ok();
        Assert.Empty(list.Items);
        Assert.Equal(0, list.TotalRecords);
    }
    
    private async Task WriteOwnFail()
    {
        var res = await _entity.Insert(_post, Name, "name1");
        Assert.True(res.IsFailed);
    }
    
    private async Task WriteOwnOk()
    {
        var res = await _entity.Insert(_post, Name,"test").Ok();
        res = await  _entity.Single(_post, res.GetProperty("id").GetInt64()).Ok();
        await _entity.Update(_post, res.ToDictionary()).Ok();
        res = await  _entity.Single(_post, res.GetProperty("id").GetInt64()).Ok();
        await _entity.Delete(_post, res.ToDictionary()).Ok();
    } 
   
    private async Task WriteOtherOk() 
    {
        var res = await _entity.Single(_post, _saPostId).Ok();
        await _entity.Update(_post, res.ToDictionary()).Ok();
    }
    
    private async Task EnsureEntityExists()
    {
        await _auth.SaDo(async () =>
        {
            await _schemaApiClient.EnsureSimpleEntity(_post, Name,false).Ok();
            var res=await _entity.Insert(_post, Name, "name1").Ok();
            _saPostId = res.GetProperty("id").GetInt64();
        });
    }

}