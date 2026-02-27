using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LisReportServer.Pages;

public class IndexModel : PageModel
{
    public string Layout { get; set; } = "_Layout";

    public IActionResult OnGet()
    {
        // 检查是否是内容模式（用于标签页iframe加载）
        if (Request.Query.ContainsKey("content") && Request.Query["content"].ToString().ToLower() == "true")
        {
            Layout = "_ContentLayout";
        }
        
        return Page();
    }
}
