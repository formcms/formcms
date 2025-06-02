using Microsoft.AspNetCore.Identity;

namespace FormCMS.Auth.Models;

public class CmsUser: IdentityUser
{
    public string? AvatarPath { get; set; }
}