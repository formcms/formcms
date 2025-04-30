using FormCMS.Auth.ApiClient;
using FormCMS.Core.Assets;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class AssetAuthTest
{
    private readonly AuthApiClient _auth;
    private readonly AccountApiClient _account;
    private readonly AssetApiClient _asset;
    private readonly (string File, byte[] Content) _file = ($"{Ulid.NewUlid()}.txt", [1, 2, 3, 4]);
    private readonly string _email = $"aa_{Ulid.NewUlid()}@cms.com";
    private readonly string _role = $"aa_{Ulid.NewUlid()}";
    private const string Pwd = $"Admin1!";
    private long _saAssetId = 0;

    public AssetAuthTest(CustomWebApplicationFactory factory)
    {
        Util.SetTestConnectionString();
        _auth = new AuthApiClient(factory.GetHttpClient());
        _account = new AccountApiClient(factory.GetHttpClient());
        _asset = new AssetApiClient(factory.GetHttpClient());
        _auth.Register(_email, Pwd).GetAwaiter().GetResult();
        AddSaAssetAndGetId().GetAwaiter().GetResult();
    }

    [Fact]
    public Task AnonymousUser() => ReadAddFail();

    [Fact]
    public async Task EmptyUser() => await _auth.Sudo(_email, Pwd, ReadAddFail);

    [Fact]
    public async Task UserRestrictedRead()
    {
        await _auth.SaDo(() => _account.AssignEntityToUser(_email, restrictedRead: [Assets.Entity.Name]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
    }

    [Fact]
    public async Task RoleRestrictedRead()
    {
        await _auth.SaDo(() => _account.AssignEntityToUserByRole(_email, _role, restrictedRead: [Assets.Entity.Name]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
    }

    [Fact]
    public async Task UserFullRead()
    {
        await _auth.SaDo( () => _account.AssignEntityToUser(_email, fullRead: [Assets.Entity.Name]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task RoleFullRead()
    {
        await _auth.SaDo( () => _account.AssignEntityToUserByRole(_email,_role, fullRead: [Assets.Entity.Name]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnFail);
        await _auth.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task UserWithRestrictedWrite()
    {
        await _auth.SaDo( () => _account.AssignEntityToUser(_email, restrictedWrite: [Assets.Entity.Name]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
        await _auth.Sudo(_email, Pwd, WriteOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOtherFail);
    }

    [Fact]
    public async Task RoleWithRestrictedWrite()
    {
        await _auth.SaDo( () => _account.AssignEntityToUserByRole(_email,_role, restrictedWrite: [Assets.Entity.Name]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, ReadOtherFail);
        await _auth.Sudo(_email, Pwd, WriteOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOtherFail);
    }

    [Fact]
    public async Task UserFullWrite()
    {
        await _auth.SaDo(() => _account.AssignEntityToUser(_email, fullWrite: [Assets.Entity.Name]));
        await _auth.Sudo(_email, Pwd, ReadOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOwnOk);
        await _auth.Sudo(_email, Pwd, WriteOtherOk);
        await _auth.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task RoleFullWrite()
    {
        await _auth.SaDo(() => _account.AssignEntityToUser(_email, fullWrite: [Assets.Entity.Name]));
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
        var res = await _asset.AddAsset([_file]);
        Assert.True(res.IsFailed);

        var singleRes = await _asset.Single(_saAssetId);
        Assert.True(singleRes.IsFailed);

        var listRes = await _asset.List(false, "");
        Assert.True(listRes.IsFailed);
    }

    private async Task ReadOtherFail() 
    {
        var res = await _asset.Single(_saAssetId);
        Assert.True(res.IsFailed);
        
        var list = await _asset.List(false, "").Ok();
        Assert.Empty(list.Items);
        Assert.Equal(0, list.TotalRecords);
    }
    
    private async Task ReadOtherOk() 
    {
        await _asset.Single(_saAssetId).Ok();
        var list = await _asset.List(false, "").Ok();
        Assert.True(list.Items.Length > 0);
        Assert.True(list.TotalRecords > 0);
    }

    private async Task ReadOwnOk()
    {
        await _asset.List(false, "").Ok();
    }
    
    private async Task WriteOwnFail()
    {
        var res = await _asset.AddAsset([_file]);
        Assert.True(res.IsFailed);
    }
    
    private async Task WriteOwnOk()
    {
        await _asset.AddAsset([_file]).Ok();
        var id = await _asset.GetAssetIdByName(_file.File);
        await _asset.Replace(id, _file.File, _file.Content).Ok();
        var asset = await _asset.Single(id).Ok();
        asset = asset with { Title = asset.Title + "aaa" };
        await _asset.UpdateMeta(asset).Ok();
    }

    private async Task WriteOtherOk()
    {
        await _asset.Replace(_saAssetId, _file.File, _file.Content).Ok();
        var asset = await _asset.Single(_saAssetId).Ok();
        await _asset.UpdateMeta(asset).Ok();
    }

    private async Task WriteOtherFail() 
    {
        //cannot replace
        var res = await _asset.Replace(_saAssetId, _file.File, _file.Content);
        Assert.True(res.IsFailed);

        //cannot update meta
        var asset = new Asset("", "", "", "test title", 10, "image/jpg", new Dictionary<string, object>(), "",
            Id: _saAssetId);
        res = await _asset.UpdateMeta(asset);
        Assert.True(res.IsFailed);
    }

    private Task AddSaAssetAndGetId() => _auth.SaDo(async () => { _saAssetId = await _asset.AddAssetAndGetId(_file); });
}