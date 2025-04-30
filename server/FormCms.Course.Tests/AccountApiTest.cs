using FormCMS.Auth.ApiClient;
using FormCMS.Core.Identities;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;

[Collection("API")]
public class AccountApiTest
{
    private readonly AuthApiClient _authApiClient;
    private readonly AccountApiClient _accountApiClient;

    private readonly string _email = $"at_user_{Ulid.NewUlid()}@cms.com";
    private readonly string _role = $"at_role_{Ulid.NewUlid()}";

    public AccountApiTest(CustomWebApplicationFactory factory)
    {
        Util.SetTestConnectionString();
        _authApiClient = new AuthApiClient(factory.GetHttpClient());
        _accountApiClient = new AccountApiClient(factory.GetHttpClient());
    }

    [Fact]
    public async Task GetEntities()
    {
        await _authApiClient.EnsureSaLogin().Ok();
        var entities = await _accountApiClient.GetEntities().Ok();
        Assert.NotEmpty(entities);
    }
    
    [Fact]
    public async Task GetUsers()
    {
        await _authApiClient.RegisterAndLogin(_email, "Admin!1");
        await _authApiClient.SaDo(async () =>
        {
            var users = await _accountApiClient.GetUsers().Ok();
            Assert.NotEmpty(users);
        });
    }
    
    [Fact]
    public async Task SingleUser()
    {
        await _authApiClient.RegisterAndLogin(_email, "Admin!1");
        await _authApiClient.SaDo(async () =>
        {
            var user = await _accountApiClient.GetSingleUserByEmail(_email);
            Assert.NotNull(user);
        });
    }

    [Fact]
    public async Task DeleteUser()
    {
        await _authApiClient.RegisterAndLogin(_email, "Admin!1");
        await _authApiClient.SaDo(async () =>
        {
            var user = await _accountApiClient.GetSingleUserByEmail(_email).Ok();
            await _accountApiClient.DeleteUser(user.Id).Ok();
            var res = await _accountApiClient.GetSingleUserByEmail(_email);
            Assert.True(res.IsFailed);
        });
    }

    [Fact]
    public async Task GetRoles()
    {
        await _authApiClient.EnsureSaLogin().Ok();
        var roles = await _accountApiClient.GetRoles().Ok();
        Assert.NotEmpty(roles);
    }

    [Fact]
    public async Task AddGetSingleDelete()
    {
        await _authApiClient.EnsureSaLogin().Ok();

        var role = new RoleAccess(_role, [], [], [], []);
        await _accountApiClient.SaveRole(role).Ok();

        role = await _accountApiClient.GetRole(_role).Ok();
        Assert.NotNull(role);

        await _accountApiClient.DeleteRole(role.Name).Ok();     

        var res = await _accountApiClient.GetRole(_role);
        Assert.True(res.IsFailed);
    }
}