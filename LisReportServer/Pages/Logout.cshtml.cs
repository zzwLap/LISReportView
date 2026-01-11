using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LisReportServer.Pages
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGet()
        {
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync();
            }
            
            return RedirectToPage("/Index");
        }
    }
}