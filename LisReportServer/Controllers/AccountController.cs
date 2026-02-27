using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using LisReportServer.Models;
using LisReportServer.Services;

namespace LisReportServer.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ISSOHealthCheckService _ssoHealthCheckService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration configuration, ISSOHealthCheckService ssoHealthCheckService, ILogger<AccountController> logger)
        {
            _configuration = configuration;
            _ssoHealthCheckService = ssoHealthCheckService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            var ssoSettings = _configuration.GetSection("SSOSettings").Get<SSOSettings>();
            
            if (ssoSettings?.Enabled == true)
            {
                var isSSOAvailable = await _ssoHealthCheckService.IsSSOAvailableAsync();
                if (isSSOAvailable)
                {
                    // SSO可用，使用SSO登录
                    var props = new AuthenticationProperties
                    {
                        RedirectUri = returnUrl ?? "~/"
                    };
                    return Challenge(props, "OpenIdConnect");
                }
                else
                {
                    // SSO不可用，降级到本地登录
                    _logger.LogWarning("SSO认证中心不可用，降级到本地登录");
                    TempData["ErrorMessage"] = "SSO认证中心当前不可用，系统将使用本地登录。";
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        Response.Cookies.Append("ReturnUrl", returnUrl);
                    }
                    return LocalRedirect("~/Login");
                }
            }
            else
            {
                // 使用本地登录
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    Response.Cookies.Append("ReturnUrl", returnUrl);
                }
                return LocalRedirect("~/Login"); // 重定向到本地登录页面
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            var ssoSettings = _configuration.GetSection("SSOSettings").Get<SSOSettings>();
            if (ssoSettings?.Enabled == true)
            {
                // 如果启用了SSO，也登出SSO
                var props = new AuthenticationProperties
                {
                    RedirectUri = "~/"
                };
                return SignOut(props, "OpenIdConnect", "Cookies");
            }
            
            return LocalRedirect("~/");
        }
    }
}