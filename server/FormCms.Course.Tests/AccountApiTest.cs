using FormCMS.Core.Identities;
using FormCMS.Utils.ResultExt;
using NUlid;

namespace FormCMS.Course.Tests;

[Collection("API")]
public class AccountApiTest(AppFactory factory)
{
    private readonly string _email = $"at_user_{Ulid.NewUlid()}@cms.com";
    private readonly string _role = $"at_role_{Ulid.NewUlid()}";

    [Fact]
    public async Task GetEntities()
    {
        await factory.AuthApi.EnsureSaLogin().Ok();
        var entities = await factory.AccountApi.GetEntities().Ok();
        Assert.NotEmpty(entities);
    }
    
    [Fact]
    public async Task GetUsers()
    {
        await  factory.AuthApi.RegisterAndLogin(_email.Split('@')[0],_email, "Admin!1");
        await  factory.AuthApi.SaDo(async () =>
        {
            var users = await factory.AccountApi.GetUsers().Ok();
            Assert.NotEmpty(users);
        });
    }
    
    [Fact]
    public async Task SingleUser()
    {
        await  factory.AuthApi.RegisterAndLogin(_email.Split('@')[0],_email, "Admin!1");
        await  factory.AuthApi.SaDo(async () =>
        {
            var user = await factory.AccountApi.GetSingleUserByEmail(_email);
            Assert.NotNull(user);
        });
    }

    [Fact]
    public async Task DeleteUser()
    {
        await  factory.AuthApi.RegisterAndLogin(_email.Split("@")[0],_email, "Admin!1");
        await  factory.AuthApi.SaDo(async () =>
        {
            var user = await factory.AccountApi.GetSingleUserByEmail(_email).Ok();
            await factory.AccountApi.DeleteUser(user.Id).Ok();
            var res = await factory.AccountApi.GetSingleUserByEmail(_email);
            Assert.True(res.IsFailed);
        });
    }

    [Fact]
    public async Task GetRoles()
    {
        await  factory.AuthApi.EnsureSaLogin().Ok();
        var roles = await factory.AccountApi.GetRoles().Ok();
        Assert.NotEmpty(roles);
    }

    [Fact]
    public async Task AddGetSingleDelete()
    {
        await  factory.AuthApi.EnsureSaLogin().Ok();

        var role = new RoleAccess(_role, [], [], [], []);
        await factory.AccountApi.SaveRole(role).Ok();

        role = await factory.AccountApi.GetRole(_role).Ok();
        Assert.NotNull(role);

        await factory.AccountApi.DeleteRole(role.Name).Ok();     

        var res = await factory.AccountApi.GetRole(_role);
        Assert.True(res.IsFailed);
    }
}