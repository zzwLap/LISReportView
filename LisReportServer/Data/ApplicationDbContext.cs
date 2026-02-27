using Microsoft.EntityFrameworkCore;
using LisReportServer.Models;

namespace LisReportServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<HospitalProfile> HospitalProfiles { get; set; }
        public DbSet<HospitalServiceConfig> HospitalServiceConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //配置User实体
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.HasIndex(e => e.Username)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Username");

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.Property(e => e.FullName)
                    .HasMaxLength(100);

                entity.Property(e => e.HospitalName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // 配置Role实体
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_Roles_Name");

                entity.Property(e => e.Description)
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            //配置UserRole实体
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => new { e.UserId, e.RoleId })
                    .IsUnique()
                    .HasDatabaseName("IX_UserRoles_UserId_RoleId");

                entity.Property(e => e.AssignedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置HospitalProfile实体
            modelBuilder.Entity<HospitalProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.HospitalName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(e => e.HospitalName)
                    .IsUnique()
                    .HasDatabaseName("IX_HospitalProfile_HospitalName");

                entity.Property(e => e.HospitalCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.HospitalCode)
                    .IsUnique()
                    .HasDatabaseName("IX_HospitalProfile_HospitalCode");

                entity.Property(e => e.ShortName)
                    .HasMaxLength(100);

                entity.Property(e => e.Address)
                    .HasMaxLength(500);

                entity.Property(e => e.ContactPhone)
                    .HasMaxLength(50);

                entity.Property(e => e.ContactEmail)
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.Logo)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // 配置HospitalServiceConfig实体
            modelBuilder.Entity<HospitalServiceConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.ServiceName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ServiceCategory)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => e.ServiceCategory)
                    .HasDatabaseName("IX_HospitalServiceConfig_ServiceCategory");

                entity.Property(e => e.ServiceDiscoveryKey)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(e => new { e.HospitalProfileId, e.ServiceDiscoveryKey })
                    .IsUnique()
                    .HasDatabaseName("IX_HospitalServiceConfig_HospitalProfileId_ServiceDiscoveryKey");

                entity.Property(e => e.ServiceAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.ApiVersion)
                    .HasMaxLength(20);

                entity.Property(e => e.AuthType)
                    .HasMaxLength(50);

                entity.Property(e => e.Username)
                    .HasMaxLength(100);

                entity.Property(e => e.EncryptedPassword)
                    .HasMaxLength(500);

                entity.Property(e => e.ApiKey)
                    .HasMaxLength(500);

                entity.Property(e => e.HealthCheckUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // 配置外键关系
                entity.HasOne(e => e.HospitalProfile)
                    .WithMany(e => e.ServiceConfigs)
                    .HasForeignKey(e => e.HospitalProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}