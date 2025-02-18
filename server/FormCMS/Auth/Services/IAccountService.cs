using FluentResults;
using FormCMS.Core.Identities;

namespace FormCMS.Auth.Services;

//manage user, role
public interface IAccountService
{
    Task<string[]> GetEntities(CancellationToken ct);
    Task<UserAccess> GetSingleUser(string id,CancellationToken ct);
    Task<UserAccess[]> GetUsers(CancellationToken ct);
    Task<string[]> GetRoles(CancellationToken ct);
    Task<Result> EnsureUser(string email, string password, string[] roles);
    Task DeleteUser(string id);
    Task SaveUser(UserAccess userAccess);
    Task<RoleAccess> GetSingleRole(string id);
    Task SaveRole(RoleAccess roleAccess);
    Task DeleteRole(string name);
}