using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LisReportServer.Pages
{
    public class AccessDeniedModel : PageModel
    {
        public string? Message { get; set; }

        public void OnGet(string? message = null)
        {
            Message = message;
        }
    }
}
