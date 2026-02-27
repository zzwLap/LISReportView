using BCrypt.Net;
using LisReportServer.Data;
using LisReportServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LisReportServer.Services
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserAuthenticationService> _logger;

        public UserAuthenticationService(ApplicationDbContext context, ILogger<UserAuthenticationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, string hospitalName)
        {
            try
            {
                // 检查医院配置是否存在且启用
                var hospitalProfile = await _context.HospitalProfiles
                    .FirstOrDefaultAsync(h => h.HospitalName == hospitalName && h.IsActive);

                if (hospitalProfile == null)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "指定的医院配置不存在或未启用"
                    };
                }

                // 获取用户信息
                var user = await GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "用户不存在"
                    };
                }

                // 检查用户是否属于该医院
                if (user.HospitalName != hospitalName)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "用户不属于指定医院"
                    };
                }

                // 检查用户是否激活
                if (!user.IsActive)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "用户账户已被禁用"
                    };
                }

                //验证密码
                if (!ValidatePassword(password, user.PasswordHash))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "密码错误"
                    };
                }

                // 获取用户角色
                var roles = await GetUserRolesAsync(user.Id);

                // 更新最后登录时间
                await UpdateLastLoginTimeAsync(user.Id);

                _logger.LogInformation("用户 {Username} 登录成功，医院: {HospitalName}", username, hospitalName);

                return new AuthenticationResult
                {
                    Success = true,
                    User = user,
                    Roles = roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户认证过程中发生错误");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "认证服务暂时不可用"
                };
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<List<Role>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null) return false;

            return ValidatePassword(password, user.PasswordHash);
        }

        public async Task UpdateLastLoginTimeAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private bool ValidatePassword(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密码验证过程中发生错误");
                return false;
            }
        }

        //辅助方法：创建密码哈希
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}