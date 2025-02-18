using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Core.Identities;

namespace FormCMS.Auth.ApiClient;

public class AccountApiClient(HttpClient client)
{
   

    public async Task<Result<string[]>> GetEntities()
        =>await client.GetResult<string[]>("/api/accounts/entities" );
    public async Task<Result<string[]>> GetRoles()
        =>await client.GetResult<string[]>("/api/accounts/roles" );
    public async Task<Result<UserAccess[]>> GetUsers()
        =>await client.GetResult<UserAccess[]>("/api/accounts/users" );
    public async Task<Result<UserAccess>> GetSingleUsers(string userId)
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
}