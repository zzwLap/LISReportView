using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LisReportServer.Services;
using System.Security.Claims;

namespace LisReportServer.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ICookieService _cookieService;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public LogoutModel(ICookieService cookieService, ITokenBlacklistService tokenBlacklistService)
        {
            _cookieService = cookieService;
            _tokenBlacklistService = tokenBlacklistService;
        }
        
        public async Task<IActionResult> OnGet()
        {
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                // 获取用户的会话ID
                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = HttpContext.User.FindFirst("SessionId")?.Value;
                
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionId))
                {
                    // 将当前会话加入黑名单
                    var sessionTokenId = $"{userId}:{sessionId}";
                    await _tokenBlacklistService.AddTokenToBlacklistAsync(sessionTokenId, DateTime.UtcNow.AddHours(24));
                }
                
                await HttpContext.SignOutAsync();
            }
            
            // 清除记住密码的Cookie
            _cookieService.ClearRememberMeCookie();
            
            return RedirectToPage("/Index");
        }
    }
}