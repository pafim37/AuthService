using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database
{
    public class AuthContext : DbContext
    {
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<RoleEntity> Roles { get; set; }
        public DbSet<PrivilegeEntity> Privileges { get; set; }

        public AuthContext(DbContextOptions options) : base(options)
        {
             Database.EnsureCreated();
        }

    }
}
