using SSOAuthCenter.Models;

namespace SSOAuthCenter.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> ValidateUserAsync(string username, string password);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> CreateUserAsync(string username, string email, string password);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);
        Task<bool> AssignRoleToUserAsync(int userId, int roleId);
        Task<bool> RevokeRoleFromUserAsync(int userId, int roleId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<bool> UpdateUserAsync(int userId, string email, string? firstName, string? lastName, bool isActive, bool isEmailConfirmed);
    }
}