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
        private readonly IThirdPartyLoginService _thirdPartyLoginService;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(
            ICookieService cookieService, 
            ITokenBlacklistService tokenBlacklistService,
            IThirdPartyLoginService thirdPartyLoginService,
            ILogger<LogoutModel> logger)
        {
            _cookieService = cookieService;
            _tokenBlacklistService = tokenBlacklistService;
            _thirdPartyLoginService = thirdPartyLoginService;
            _logger = logger;
        }
        
        public async Task<IActionResult> OnGet()
        {
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                // 获取用户信息
                var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                var hospitalName = HttpContext.User.FindFirst("HospitalName")?.Value;
                var isLocalUser = HttpContext.User.FindFirst("IsLocalUser")?.Value;
                var sessionId = HttpContext.User.FindFirst("SessionId")?.Value;
                        
                // 将当前会话加入黑名单
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionId))
                {
                    var sessionTokenId = $"{userId}:{sessionId}";
                    await _tokenBlacklistService.AddTokenToBlacklistAsync(sessionTokenId, DateTime.UtcNow.AddHours(24));
                }
        
                // 如果是第三方（LIS）用户，清除LIS Token缓存
                if (isLocalUser == "False" && !string.IsNullOrEmpty(hospitalName) && !string.IsNullOrEmpty(username))
                {
                    try
                    {
                        await _thirdPartyLoginService.ClearUserTokenAsync(hospitalName, username);
                        _logger.LogInformation("已清除LIS用户Token缓存: 医院={HospitalName}, 用户={Username}", hospitalName, username);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清除LIS Token缓存失败: 医院={HospitalName}, 用户={Username}", hospitalName, username);
                    }
                }
                        
                await HttpContext.SignOutAsync();
            }
                    
            // 清除记住密码的Cookie
            _cookieService.ClearRememberMeCookie();
                    
            return RedirectToPage("/Index");
        }
    }
}