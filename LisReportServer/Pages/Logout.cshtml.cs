using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LisReportServer.Services;

namespace LisReportServer.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ICookieService _cookieService;

        public LogoutModel(ICookieService cookieService)
        {
            _cookieService = cookieService;
        }
        
        public async Task<IActionResult> OnGet()
        {
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync();
            }
            
            // 清除记住密码的Cookie
            _cookieService.ClearRememberMeCookie();
            
            return RedirectToPage("/Index");
        }
    }
}