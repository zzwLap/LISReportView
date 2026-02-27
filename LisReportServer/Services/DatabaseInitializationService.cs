using LisReportServer.Data;
using LisReportServer.Models;
using LisReportServer.Services;
using Microsoft.EntityFrameworkCore;

namespace LisReportServer.Services
{
    public class DatabaseInitializationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(ApplicationDbContext context, ILogger<DatabaseInitializationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                //确保数据库已创建
                await _context.Database.EnsureCreatedAsync();

                // 初始化默认数据
                await InitializeDefaultRolesAsync();
                await InitializeDefaultUsersAsync();
                await InitializeDefaultHospitalConfigsAsync();

                _logger.LogInformation("数据库初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库初始化过程中发生错误");
                throw;
            }
        }

        private async Task InitializeDefaultRolesAsync()
        {
            //检查是否已存在角色
            if (await _context.Roles.AnyAsync())
                return;

            var roles = new[]
            {
                new Role { Name = "Admin", Description = "系统管理员" },
                new Role { Name = "Doctor", Description = "医生" },
                new Role { Name = "Nurse", Description = "护士" },
                new Role { Name = "Technician", Description = "技师" }
            };

            _context.Roles.AddRange(roles);
            await _context.SaveChangesAsync();

            _logger.LogInformation("默认角色初始化完成");
        }

        private async Task InitializeDefaultUsersAsync()
        {
            //检查是否已存在用户
            if (await _context.Users.AnyAsync())
                return;

            // 获取系统角色
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                _logger.LogWarning("未找到管理员角色");
                return;
            }

            // 创建默认管理员用户
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = UserAuthenticationService.HashPassword("admin123"),
                FullName = "系统管理员",
                HospitalName = "系统",
                Email = "admin@system.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            // 为管理员用户分配角色
            var userRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("默认用户初始化完成");
        }

        private async Task InitializeDefaultHospitalConfigsAsync()
        {
            //检查是否已存在医院配置
            if (await _context.HospitalProfiles.AnyAsync(h => h.HospitalName == "系统"))
                return;

            // 创建系统医院配置
            var systemHospital = new HospitalProfile
            {
                HospitalName = "系统",
                HospitalCode = "SYSTEM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HospitalProfiles.Add(systemHospital);
            await _context.SaveChangesAsync();

            _logger.LogInformation("默认医院配置初始化完成");
        }
    }
}