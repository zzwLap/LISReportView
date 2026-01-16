using Microsoft.EntityFrameworkCore;
using SSOAuthCenter.Data;
using SSOAuthCenter.Models;
using SSOAuthCenter.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SSOAuthCenter.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);

            if (user == null || user.PasswordHash == null)
                return null;

            var hashedPassword = HashPassword(password, user.Email); // 使用邮箱作为盐值
            
            return hashedPassword == user.PasswordHash ? user : null;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
        }

        public async Task<bool> CreateUserAsync(string username, string email, string password)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email.ToLower());

            if (existingUser != null)
                return false;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password, email), // 使用邮箱作为盐值
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 默认分配普通用户角色
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = 2, // 普通用户角色
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.PasswordHash == null)
                return false;

            var currentHashedPassword = HashPassword(currentPassword, user.Email);
            if (currentHashedPassword != user.PasswordHash)
                return false;

            user.PasswordHash = HashPassword(newPassword, user.Email);
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Role>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => r)
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

            if (user == null || role == null)
                return false;

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole != null)
                return true; // 角色已分配

            userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RevokeRoleFromUserAsync(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<bool> UpdateUserAsync(int userId, string email, string? firstName, string? lastName, bool isActive, bool isEmailConfirmed)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // 检查邮箱是否已被其他用户使用
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId);
            if (existingUser != null)
            {
                return false;
            }

            user.Email = email;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.IsActive = isActive;
            user.IsEmailConfirmed = isEmailConfirmed;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}