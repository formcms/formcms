using FormCMS.Auth.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SqlVideoWebApp;


internal class CmsDbContext : IdentityDbContext<CmsUser>
{
    public CmsDbContext() { }
    public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options) { }
}