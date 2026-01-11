using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using LisReportServer.Services;

namespace LisReportServer.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ICookieService _cookieService;

        public LoginModel(ICookieService cookieService)
        {
            _cookieService = cookieService;
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
                // 这里应该调用实际的身份验证逻辑
                // 模拟验证（在实际应用中，应连接数据库或API验证凭据）
                var isValidUser = await ValidateUserAsync(Input.HospitalName, Input.Username, Input.Password);

                if (isValidUser)
                {
                    // 创建用户声明
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("HospitalName", Input.HospitalName),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, Input.Username),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Input.Username),
                        new System.Security.Claims.Claim("LoginTime", DateTime.Now.ToString())
                    };

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
                    ModelState.AddModelError(string.Empty, "无效的登录尝试。");
                }
            }

            // 如果到达这里，则有错误，重新显示表单
            return Page();
        }

        private async Task<bool> ValidateUserAsync(string hospitalName, string username, string password)
        {
            // 模拟用户验证逻辑
            // 在实际应用中，这里应该查询数据库或调用认证服务
            await Task.Delay(100); // 模拟异步操作延迟

            // 示例验证：允许任意医院名称，但用户名必须是预设的有效用户，密码对应
            // 实际应用中应替换为真实的身份验证逻辑
            if (username == "admin" && password == "password")
            {
                return true;
            }
            
            // 添加更多示例用户
            if (username == "doctor" && password == "doc123")
            {
                return true;
            }
            
            if (username == "nurse" && password == "nurse456")
            {
                return true;
            }
            
            return false;
        }
    }
}