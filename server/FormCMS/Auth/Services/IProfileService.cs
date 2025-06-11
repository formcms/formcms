using FormCMS.Auth.Models;
using System.Security.Claims;

namespace FormCMS.Auth.Services;

public interface IProfileService
{
    AccessLevel MustGetReadWriteLevel(string entityName);
    AccessLevel MustGetReadLevel(string entityName);
    Task ChangePassword(string password, string newPassword);
    void MustHasAnyRole(IEnumerable<string> role);
    Task EnsureCurrentUserHaveEntityAccess(string entityName);
    bool HasRole(string role);
    Task UploadAvatar(IFormFile file, CancellationToken ct);
    Task  LoadUserContext(ClaimsPrincipal user);
}