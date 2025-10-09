using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Auth.Models;

public class CmsDbContext:IdentityDbContext<CmsUser>
{
    public CmsDbContext() { }
    public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options) { } 
}