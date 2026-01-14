using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace LisReportServer.Services
{
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public CustomCookieAuthenticationEvents(ITokenBlacklistService tokenBlacklistService)
        {
            _tokenBlacklistService = tokenBlacklistService;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionId = context.Principal?.FindFirst("SessionId")?.Value;
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
            {
                // 如果没有用户ID或会话ID声明，则视为无效
                context.RejectPrincipal();
                return;
            }
            
            // 使用用户ID和会话ID的组合来唯一标识会话
            var sessionTokenId = $"{userId}:{sessionId}";
            
            // 检查会话ID是否在黑名单中
            var isBlacklisted = await _tokenBlacklistService.IsTokenBlacklistedAsync(sessionTokenId);
            if (isBlacklisted)
            {
                // 会话已被列入黑名单，拒绝访问
                context.RejectPrincipal();
                return;
            }
        }
    }
}