using GestionPeliculas.api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionPeliculas.api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Privilege> Privileges { get; set; }

        public DbSet<UserPrivilege> UsersPrivileges { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UserPrivilege>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPrivileges)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPrivilege>()
                .HasOne(up => up.Privilege)
                .WithMany(p => p.UserPrivileges)
                .HasForeignKey(up => up.PrivilegeId);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId);
        }
    }
}