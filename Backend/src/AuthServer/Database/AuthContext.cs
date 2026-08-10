using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database
{
    public class AuthContext : DbContext
    {
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<RoleEntity> Roles { get; set; }
        public DbSet<PrivilegeEntity> Privileges { get; set; }
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

        public AuthContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>()
                .HasIndex(user => user.Login)
                .IsUnique();

            modelBuilder.Entity<RoleEntity>()
                .HasIndex(role => role.Name)
                .IsUnique();

            modelBuilder.Entity<PrivilegeEntity>()
                .HasIndex(privilege => privilege.Name)
                .IsUnique();

            modelBuilder.Entity<RoleEntity>()
                .HasMany(role => role.Privileges)
                .WithMany();

            modelBuilder.Entity<UserEntity>()
                .HasOne(user => user.Role)
                .WithMany()
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshTokenEntity>()
                .HasIndex(refreshToken => refreshToken.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RefreshTokenEntity>()
                .HasOne(refreshToken => refreshToken.User)
                .WithMany()
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
