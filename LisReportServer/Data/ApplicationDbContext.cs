using Microsoft.EntityFrameworkCore;
using LisReportServer.Models;

namespace LisReportServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<HospitalServerConfig> HospitalServerConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置HospitalServerConfig实体
            modelBuilder.Entity<HospitalServerConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.HospitalName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.HospitalCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.HospitalCode)
                    .IsUnique()
                    .HasDatabaseName("IX_HospitalServerConfig_HospitalCode");

                entity.Property(e => e.ServerAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Username)
                    .HasMaxLength(100);

                entity.Property(e => e.EncryptedPassword)
                    .HasMaxLength(500);

                entity.Property(e => e.OtherParameters)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}