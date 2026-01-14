using Microsoft.AspNetCore.Mvc.RazorPages;
using LisReportServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace LisReportServer.Pages
{
    public class HealthStatusModel : PageModel
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILogger<HealthStatusModel> logger;

        public HealthStatusModel(IHealthCheckService healthCheckService, ILogger<HealthStatusModel> logger)
        {
            _healthCheckService = healthCheckService;
            this.logger = logger;
        }

        public HealthStatus HealthStatus { get; set; } = new HealthStatus();

        public async Task<IActionResult> OnGetAsync()
        {
            // 检查用户是否具有管理员角色
            if (!User.IsInRole("Admin") && User.Identity.Name != "admin")
            {
                TempData["ErrorMessage"] = "您没有权限访问此页面。";
                return RedirectToPage("/Index");
            }

            try
            {
                HealthStatus = await _healthCheckService.GetHealthStatusAsync();
                return Page();
            }
            catch (Exception ex)
            {
                // 记录错误但仍然显示页面
                logger?.LogError(ex, "Error retrieving health status");

                HealthStatus = new HealthStatus
                {
                    Status = "Unhealthy",
                    CheckedAt = DateTime.UtcNow,
                    ServiceName = "LIS Report Server",
                    Version = "1.0.0",
                    Components = new Dictionary<string, object>
                    {
                        { "error", new { status = "Unhealthy", message = ex.Message } }
                    }
                };

                return Page();
            }
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            // 检查用户是否具有管理员角色
            if (!User.IsInRole("Admin") && User.Identity.Name != "admin")
            {
                TempData["ErrorMessage"] = "您没有权限执行此操作。";
                return RedirectToPage("/Index");
            }
            
            return await OnGetAsync();
        }
    }
}