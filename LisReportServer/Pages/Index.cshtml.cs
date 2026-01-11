using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LisReportServer.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // 如果用户已登录，重定向到仪表板
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard");
        }
        
        return Page();
    }
}
