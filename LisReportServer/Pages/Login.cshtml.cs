using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using LisReportServer.Services;
using LisReportServer.Helpers;

namespace LisReportServer.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ICookieService _cookieService;
        private readonly IUserAuthenticationService _userAuthenticationService;
        private readonly IThirdPartyLoginService _thirdPartyLoginService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            ICookieService cookieService,
            IUserAuthenticationService userAuthenticationService,
            IThirdPartyLoginService thirdPartyLoginService,
            ILogger<LoginModel> logger)
        {
            _cookieService = cookieService;
            _userAuthenticationService = userAuthenticationService;
            _thirdPartyLoginService = thirdPartyLoginService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "请输入医院名称")]
            [Display(Name = "医院名称")]
            public string HospitalName { get; set; } = string.Empty;

            [Required(ErrorMessage = "请输入用户名")]
            [Display(Name = "用户名")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "请输入密码")]
            [DataType(DataType.Password)]
            [Display(Name = "密码")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "下次自动填写医院和用户名")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // 如果用户已经登录，则重定向到首页
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl);
            }

            // 从Cookie中获取记住的凭据
            var rememberedCredentials = _cookieService.GetRememberMeCookie();
            if (rememberedCredentials.HasValue)
            {
                Input.HospitalName = rememberedCredentials.Value.hospitalName;
                Input.Username = rememberedCredentials.Value.username;
                Input.RememberMe = true; // 默认勾选记住我
            }

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            // 检查模型状态
            if (ModelState.IsValid)
            {
                // 解密密码（假设前端已经加密）
                string actualPassword;
                try
                {
                    // 尝试解密，如果失败则认为是明文（兼容旧版本）
                    actualPassword = CryptoHelper.Decrypt(Input.Password);
                    _logger.LogDebug("密码已解密");
                }
                catch
                {
                    // 如果解密失败，说明前端没有加密，直接使用原始密码
                    actualPassword = Input.Password;
                    _logger.LogWarning("密码未加密，建议前端启用加密传输");
                }

                // 根据医院名称决定使用本地验证还是第三方验证
                bool isValidUser;
                List<string> userRoles = new List<string>();

                if (Input.HospitalName == "系统")
                {
                    // 使用本地数据库验证
                    var authResult = await _userAuthenticationService.AuthenticateAsync(
                        Input.Username, 
                        actualPassword,  // 使用解密后的密码
                        Input.HospitalName);

                    if (authResult.Success && authResult.User != null)
                    {
                        isValidUser = true;
                        userRoles = authResult.Roles.Select(r => r.Name).ToList();
                        _logger.LogInformation("本地用户 {Username} 登录成功", Input.Username);
                    }
                    else
                    {
                        isValidUser = false;
                        _logger.LogWarning("本地用户 {Username} 登录失败: {Error}", Input.Username, authResult.ErrorMessage);
                        ModelState.AddModelError(string.Empty, authResult.ErrorMessage ?? "无效的登录尝试。");
                    }
                }
                else
                {
                    // 使用第三方验证服务
                    var thirdPartyResult = await _thirdPartyLoginService.AuthenticateAsync(
                        Input.HospitalName, 
                        Input.Username, 
                        actualPassword);  // 使用解密后的密码
                    
                    if (thirdPartyResult.Success)
                    {
                        isValidUser = true;
                        // 第三方验证成功，默认给予普通用户权限
                        userRoles.Add("User");
                        _logger.LogInformation("第三方用户 {Username} 登录成功，医院: {HospitalName}", Input.Username, Input.HospitalName);
                        
                        // 可以将AccessToken和RefreshToken存储到Claims中，供后续使用
                        // 这些Token可以用于调用第三方API
                    }
                    else
                    {
                        isValidUser = false;
                        _logger.LogWarning("第三方用户 {Username} 登录失败，医院: {HospitalName}, 错误: {Error}", 
                            Input.Username, Input.HospitalName, thirdPartyResult.ErrorMessage);
                        ModelState.AddModelError(string.Empty, thirdPartyResult.ErrorMessage ?? "第三方登录失败。");
                    }
                }

                if (isValidUser)
                {
                    // 创建用户声明
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("HospitalName", Input.HospitalName),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, Input.Username),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Input.Username),
                        new System.Security.Claims.Claim("LoginTime", DateTime.Now.ToString()),
                        new System.Security.Claims.Claim("SessionId", Guid.NewGuid().ToString()),
                        new System.Security.Claims.Claim("IsLocalUser", (Input.HospitalName == "系统").ToString())
                    };

                    // 添加角色声明
                    foreach (var role in userRoles)
                    {
                        claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
                    }

                    var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
                        claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = Input.RememberMe,
                        ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    // 设置记住密码的Cookie
                    _cookieService.SetRememberMeCookie(Input.HospitalName, Input.Username, Input.RememberMe);

                    await HttpContext.SignInAsync(
                        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                        new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return LocalRedirect(returnUrl);
                }
                else
                {
                    if (!ModelState.ErrorCount.Equals(0))
                    {
                        // 已经添加了错误消息
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "无效的登录尝试。");
                    }
                }
            }

            // 如果到达这里，则有错误，重新显示表单
            return Page();
        }
    }
}