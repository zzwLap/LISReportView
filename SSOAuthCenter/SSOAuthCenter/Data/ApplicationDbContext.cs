using Microsoft.EntityFrameworkCore;
using SSOAuthCenter.Models;

namespace SSOAuthCenter.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ClientApplication> ClientApplications { get; set; }
        public DbSet<AuthToken> AuthTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置User和Role的多对多关系
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置ClientApplication的唯一性约束
            modelBuilder.Entity<ClientApplication>()
                .HasIndex(c => c.ClientId)
                .IsUnique();

            // 配置AuthToken的索引
            modelBuilder.Entity<AuthToken>()
                .HasIndex(a => a.TokenValue)
                .IsUnique();

            modelBuilder.Entity<AuthToken>()
                .HasIndex(a => a.UserId);

            // 种子数据 - 创建默认管理员角色
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Administrator", Description = "System Administrator", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Role { Id = 2, Name = "User", Description = "Regular User", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
        }
    }
}