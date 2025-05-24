using Microsoft.AspNetCore.Identity;

namespace FormCMS.Auth.Models;

public class CmsUser: IdentityUser
{
    public string? AvatarUrl { get; set; }
}