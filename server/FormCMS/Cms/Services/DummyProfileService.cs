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

    public async Task ChangePassword(string password, string newPassword)
    {
    }

    public void MustHasAnyRole(IEnumerable<string> role)
    {
    }

    public async Task EnsureCurrentUserHaveEntityAccess(string entityName)
    {
    }

    public bool HasRole(string role)
    {
        return true;
    }

    public async Task UploadAvatar(IFormFile file, CancellationToken ct)
    {
    }
}