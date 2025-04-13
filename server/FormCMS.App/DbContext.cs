using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.App;


internal class DbContext : IdentityDbContext<IdentityUser>
{
    public DbContext(){}
    public DbContext(DbContextOptions<DbContext> options):base(options){}
}