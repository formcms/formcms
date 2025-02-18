using FormCMS.Auth.ApiClient;
using FormCMS.Core.Identities;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.ResultExt;
using NUlid;
using Xunit.Abstractions;

namespace FormCMS.Course.Tests;

public class AccountApiTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AuthApiClient _authApiClient;
    private readonly AccountApiClient _accountApiClient;
    private readonly SchemaApiClient _schemaApiClient;
    private readonly EntityApiClient _entityApiClient;

    private readonly string _post = "account_api_test_post_" + Ulid.NewUlid();
    private readonly string _email = $"test1_{Ulid.NewUlid()}@cms.com";
    private readonly string _role = $"test_role_{Ulid.NewUlid()}";

    public AccountApiTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Util.SetTestConnectionString();
        var webAppClient = new WebAppClient<Program>();
        _authApiClient = new AuthApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());

    }

    [Fact]
    public async Task GetEntities()
    {
        await _authApiClient.EnsureSaLogin().Ok();
        var entities = await _accountApiClient.GetEntities().Ok();
        Assert.NotEmpty(entities);
    }

    [Fact]
    public async Task GetUsersAndSingleUser()
    {
        await _authApiClient.Register(_email, "Admin!1");
        await _authApiClient.EnsureSaLogin();
        var users = await _accountApiClient.GetUsers().Ok();
        var user = users.FirstOrDefault(x => x.Email == _email);
        Assert.NotNull(user);
        await _accountApiClient.GetSingleUsers(user.Id).Ok();
    }

    [Fact]
    public async Task DeleteUser()
    {
        await _authApiClient.Register(_email, "Admin!1");
        await _authApiClient.EnsureSaLogin();
        var users = await _accountApiClient.GetUsers().Ok();
        var user = users.FirstOrDefault(x => x.Email == _email);
        Assert.NotNull(user);
        _testOutputHelper.WriteLine(user.ToString());
        await _accountApiClient.DeleteUser(user.Id).Ok();
        users = await _accountApiClient.GetUsers().Ok();
        Assert.Null(users.FirstOrDefault(x => x.Email == _email));
    }

    [Fact]
    public async Task AssignEntityToUser()
    {
        //register a test user
        await _authApiClient.Register(_email, "Admin!1");

        //login as admin, add an entity post and give the user post permission
        await _authApiClient.EnsureSaLogin();
        await _schemaApiClient.EnsureSimpleEntity(_post, "name",false).Ok();

        var users = await _accountApiClient.GetUsers().Ok();
        var user = users.FirstOrDefault(x => x.Email == _email);
        Assert.NotNull(user);
        user = user with
        {
            RestrictedReadWriteEntities = [_post]
        };
        await _accountApiClient.SaveUser(user).Ok();
        await _authApiClient.Logout();

        //logout, so inert entity should fail
        var res = await _entityApiClient.Insert(_post, "name", "name1");
        Assert.True(res.IsFailed);

        //login as the test user
        await _authApiClient.Login(_email, "Admin!1");
        await _entityApiClient.Insert(_post, "name", "name1").Ok();
    }


    [Fact]
    public async Task AnonymousGetAll() => Assert.True((await _schemaApiClient.All(null)).IsFailed);


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

    [Fact]
    public async Task AssignRoleToUser()
    {
        //register a test user
        await _authApiClient.Register(_email, "Admin!1");

        //login as admin, add an entity post and give the user post permission
        await _authApiClient.EnsureSaLogin();
        await _schemaApiClient.EnsureSimpleEntity(_post, "name", false).Ok();
        
        //create a role with permission for `_post`
        var role = new RoleAccess(_role, [_post], [], [], []);
        await _accountApiClient.SaveRole(role).Ok();

        var users = await _accountApiClient.GetUsers().Ok();
        var user = users.FirstOrDefault(x => x.Email == _email);
        Assert.NotNull(user);

        user = user with { Roles = [role.Name] };
        await _accountApiClient.SaveUser(user).Ok();
        await _authApiClient.Logout();

        //logout, so insert entity should fail
        var res = await _entityApiClient.Insert(_post, "name", "name1");
        Assert.True(res.IsFailed);

        //login as the test user
        await _authApiClient.Login(_email, "Admin!1");
        await _entityApiClient.Insert(_post, "name", "name1").Ok();
    }

}