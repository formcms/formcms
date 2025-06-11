using FormCMS.Core.Assets;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class AssetAuthTest
{
    private readonly (string File, byte[] Content) _file = ($"{Ulid.NewUlid()}.txt", [1, 2, 3, 4]);
    private readonly string _email = $"aa_{Ulid.NewUlid()}@cms.com";
    private readonly string _role = $"aa_{Ulid.NewUlid()}";
    private const string Pwd = $"Admin1!";
    private long SaAssetId { get; }
    private AppFactory Factory { get; }

    public AssetAuthTest(AppFactory factory)
    {
        Factory = factory;
        factory.AuthApi.Register(_email.Split('@')[0],_email, Pwd).GetAwaiter().GetResult();
        SaAssetId = AddSaAssetAndGetId().GetAwaiter().GetResult();
    }

    [Fact]
    public Task AnonymousUser() => ReadAddFail();

    [Fact]
    public async Task EmptyUser() => await Factory.AuthApi.Sudo(_email, Pwd, ReadAddFail);

    [Fact]
    public async Task UserRestrictedRead()
    {
        await Factory.AuthApi.SaDo(() =>  Factory.AccountApi.AssignEntityToUser(_email, restrictedRead: [Assets.XEntity.Name]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
    }

    [Fact]
    public async Task RoleRestrictedRead()
    {
        await Factory.AuthApi.SaDo(() =>  Factory.AccountApi.AssignEntityToUserByRole(_email, _role, restrictedRead: [Assets.XEntity.Name]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
    }

    [Fact]
    public async Task UserFullRead()
    {
        await Factory.AuthApi.SaDo( () =>  Factory.AccountApi.AssignEntityToUser(_email, fullRead: [Assets.XEntity.Name]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task RoleFullRead()
    {
        await Factory.AuthApi.SaDo( () =>  Factory.AccountApi.AssignEntityToUserByRole(_email,_role, fullRead: [Assets.XEntity.Name]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnFail);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task UserWithRestrictedWrite()
    {
        await Factory.AuthApi.SaDo( () => Factory.AccountApi.AssignEntityToUser(_email, restrictedWrite: [Assets.XEntity.Name]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOtherFail);
    }

    [Fact]
    public async Task RoleWithRestrictedWrite()
    {
        await Factory.AuthApi.SaDo( () =>  Factory.AccountApi.AssignEntityToUserByRole(_email,_role, restrictedWrite: [Assets.XEntity.Name]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherFail);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOtherFail);
    }

    [Fact]
    public async Task UserFullWrite()
    {
        await Factory.AuthApi.SaDo(() =>  Factory.AccountApi.AssignEntityToUser(_email, fullWrite: [Assets.XEntity.Name]));
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOwnOk);
        await Factory.AuthApi.Sudo(_email, Pwd, WriteOtherOk);
        await Factory.AuthApi.Sudo(_email, Pwd, ReadOtherOk);
    }

    [Fact]
    public async Task RoleFullWrite()
    {
        await Factory.AuthApi.SaDo(() =>  Factory.AccountApi.AssignEntityToUser(_email, fullWrite: [Assets.XEntity.Name]));
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
        var res = await  Factory.AssetApi.AddAsset([_file]);
        Assert.True(res.IsFailed);

        var singleRes = await Factory.AssetApi.Single(SaAssetId);
        Assert.True(singleRes.IsFailed);

        var listRes = await Factory.AssetApi.List(false, "");
        Assert.True(listRes.IsFailed);
    }

    private async Task ReadOtherFail() 
    {
        var res = await Factory.AssetApi.Single(SaAssetId);
        Assert.True(res.IsFailed);
        
        var list = await Factory.AssetApi.List(false, "").Ok();
        Assert.Empty(list.Items);
        Assert.Equal(0, list.TotalRecords);
    }
    
    private async Task ReadOtherOk() 
    {
        await Factory.AssetApi.Single(SaAssetId).Ok();
        var list = await Factory.AssetApi.List(false, "").Ok();
        Assert.True(list.Items.Length > 0);
        Assert.True(list.TotalRecords > 0);
    }

    private async Task ReadOwnOk()
    {
        await Factory.AssetApi.List(false, "").Ok();
    }
    
    private async Task WriteOwnFail()
    {
        var res = await Factory.AssetApi.AddAsset([_file]);
        Assert.True(res.IsFailed);
    }
    
    private async Task WriteOwnOk()
    {
        await Factory.AssetApi.AddAsset([_file]).Ok();
        var id = await Factory.AssetApi.GetAssetIdByName(_file.File);
        await Factory.AssetApi.Replace(id, _file.File, _file.Content).Ok();
        var asset = await Factory.AssetApi.Single(id).Ok();
        asset = asset with { Title = asset.Title + "aaa" };
        await Factory.AssetApi.UpdateMeta(asset).Ok();
    }

    private async Task WriteOtherOk()
    {
        await Factory.AssetApi.Replace(SaAssetId, _file.File, _file.Content).Ok();
        var asset = await Factory.AssetApi.Single(SaAssetId).Ok();
        await Factory.AssetApi.UpdateMeta(asset).Ok();
    }

    private async Task WriteOtherFail() 
    {
        //cannot replace
        var res = await Factory.AssetApi.Replace(SaAssetId, _file.File, _file.Content);
        Assert.True(res.IsFailed);

        //cannot update meta
        var asset = new Asset("", "", "", "test title", 10, "image/jpg", new Dictionary<string, object>(), "",
            Id: SaAssetId);
        res = await Factory.AssetApi.UpdateMeta(asset);
        Assert.True(res.IsFailed);
    }

    private async Task<long> AddSaAssetAndGetId()
    {
        var ret = 0L;
        await Factory.AuthApi.SaDo(async () =>
        {
            ret = await Factory.AssetApi.AddAssetAndGetId(_file);
        });
        return ret;
    }
}