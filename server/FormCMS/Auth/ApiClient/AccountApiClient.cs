using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Core.Identities;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.ApiClient;

public class AccountApiClient(HttpClient client)
{
    public async Task<Result<string[]>> GetEntities()
        =>await client.GetResult<string[]>("/api/accounts/entities" );
    public async Task<Result<string[]>> GetRoles()
        =>await client.GetResult<string[]>("/api/accounts/roles" );
    public async Task<Result<UserAccess[]>> GetUsers()
        =>await client.GetResult<UserAccess[]>("/api/accounts/users" );
    public async Task<Result<UserAccess>> GetSingleUser(string userId)
        =>await client.GetResult<UserAccess>($"/api/accounts/users/{userId}" );
    public async Task<Result> DeleteUser(string userId)
        =>await client.DeleteResult($"/api/accounts/users/{userId}");

    public async Task<Result> SaveUser(UserAccess userAccess)
        => await client.PostResult($"/api/accounts/users", userAccess);
    
    public async Task<Result> SaveRole(RoleAccess roleAccess)
        => await client.PostResult($"/api/accounts/roles", roleAccess);
    
    public async Task<Result<RoleAccess>> GetRole(string role)
        => await client.GetResult<RoleAccess>($"/api/accounts/roles/{role}");
    public async Task<Result> DeleteRole(string role)
        => await client.DeleteResult($"/api/accounts/roles/{role}");

    public async Task AssignEntityToUserByRole(string email, string roleName, string[]? fullWrite = null, string[]? restrictedWrite = null,
        string[]? fullRead = null, string[]? restrictedRead = null)
    {
        var role = new RoleAccess(
            roleName,
            ReadWriteEntities: fullWrite ?? [],
            RestrictedReadWriteEntities: restrictedWrite ?? [],
            ReadonlyEntities: fullRead ?? [],
            RestrictedReadonlyEntities: restrictedRead ?? []
        );
        await SaveRole(role).Ok();
        
        var users = await GetUsers().Ok();
        var user = users.FirstOrDefault(x => x.Email == email) ?? throw new Exception();
        user = user with
        {
            Roles = [roleName],
            ReadonlyEntities = [],
            RestrictedReadonlyEntities = [],
            ReadWriteEntities = [],
            RestrictedReadWriteEntities = []
        };

        await SaveUser(user).Ok();
    }

    public async Task AssignEntityToUser(string email, string[]? fullWrite = null, string[]? restrictedWrite = null, string[]? fullRead = null, string[]? restrictedRead = null)
    {
        var users = await GetUsers().Ok();
        var user = users.FirstOrDefault(x => x.Email == email)?? throw new Exception();
        user = user with
        {
            Roles = [],
            ReadonlyEntities = fullRead??[],
            RestrictedReadonlyEntities = restrictedRead??[],

            ReadWriteEntities = fullWrite??[],
            RestrictedReadWriteEntities = restrictedWrite??[]
        };
        
        await SaveUser(user).Ok();
    }

    public async Task<Result<UserAccess>> GetSingleUserByEmail(string email)
    {
        var users = await GetUsers().Ok();
        var user = users.FirstOrDefault(x => x.Email == email);
        if (user == null)
        {
            return Result.Fail("no user found");
            
        }
        return await GetSingleUser(user.Id);
    }
}