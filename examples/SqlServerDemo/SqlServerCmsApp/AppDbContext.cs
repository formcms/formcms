using FormCMS.Auth.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CmsApp;

public class AppDbContext:IdentityDbContext<CmsUser>
{
    public AppDbContext() { }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}