using FormCMS.Auth.Models;
using FormCMS.Auth.Services;

namespace FormCMS.Cms.Services;

public class DummyProfileService: IProfileService
{
    public AccessLevel MustGetReadWriteLevel(string entityName)
    {
        return AccessLevel.Full;
    }

    public AccessLevel MustGetReadLevel(string entityName)
    {
        return AccessLevel.Full;
    }

    public Task ChangePassword(string password, string newPassword)
    {
        return Task.CompletedTask;
    }

    public void MustHasAnyRole(IEnumerable<string> role)
    {
    }

    public Task EnsureCurrentUserHaveEntityAccess(string entityName)
    {
        return Task.CompletedTask;
    }

    public bool HasRole(string role)
    {
        return true;
    }

    public Task UploadAvatar(IFormFile file, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}