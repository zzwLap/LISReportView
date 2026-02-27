using LisReportServer.Models;

namespace LisReportServer.Services
{
    public interface IUserAuthenticationService
    {
        /// <summary>
        ///验证用户凭据
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="hospitalName">医院名称</param>
        /// <returns>认证结果</returns>
        Task<AuthenticationResult> AuthenticateAsync(string username, string password, string hospitalName);

        /// <summary>
        ///根据用户名获取用户信息
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户信息</returns>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>角色列表</returns>
        Task<List<Role>> GetUserRolesAsync(int userId);

        /// <summary>
        ///验证密码
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>密码是否正确</returns>
        Task<bool> ValidatePasswordAsync(string username, string password);

        /// <summary>
        /// 更新用户最后登录时间
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task UpdateLastLoginTimeAsync(int userId);
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public List<Role> Roles { get; set; } = new();
    }
}