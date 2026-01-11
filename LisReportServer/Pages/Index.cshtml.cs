using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LisReportServer.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // 首页现在支持显示已登录和未登录两种状态
        // 如果用户已登录，可以在页面上显示进入仪表板的按钮
        // 不再直接重定向，而是显示首页内容
        return Page();
    }
}
