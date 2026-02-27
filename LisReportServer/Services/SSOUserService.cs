using System.Security.Claims;

namespace LisReportServer.Services
{
    public interface ISSOUserService
    {
        Task<string> GetOrCreateLocalUserFromSSOAsync(ClaimsPrincipal ssoUser);
    }

    public class SSOUserService : ISSOUserService
    {
        public async Task<string> GetOrCreateLocalUserFromSSOAsync(ClaimsPrincipal ssoUser)
        {
            // 从SSO用户信息中提取用户标识
            var ssoUserId = ssoUser.FindFirst("sub")?.Value;
            var name = ssoUser.FindFirst("name")?.Value ?? ssoUser.Identity.Name;
            var email = ssoUser.FindFirst("email")?.Value;

            // 实现用户映射逻辑
            // 这里可以根据需要创建或查找本地用户
            // 返回本地用户ID或名称
            return name ?? "unknown"; // 简化实现，实际应用中需要更复杂的逻辑
        }
    }
}