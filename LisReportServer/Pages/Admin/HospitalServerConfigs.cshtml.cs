using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LisReportServer.Models;
using LisReportServer.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace LisReportServer.Pages.Admin
{
    [ValidateAntiForgeryToken]
    public class HospitalServerConfigsModel : PageModel
    {
        private readonly IHospitalServerConfigService _hospitalServerConfigService;

        public HospitalServerConfigsModel(IHospitalServerConfigService hospitalServerConfigService)
        {
            _hospitalServerConfigService = hospitalServerConfigService;
        }

        [BindProperty]
        public List<HospitalServerConfig> Configs { get; set; } = new();

        [BindProperty]
        public HospitalServerConfig Config { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 检查用户是否具有管理员角色
            if (!User.IsInRole("Admin") && User.Identity.Name != "admin")
            {
                TempData["ErrorMessage"] = "您没有权限访问此页面。";
                return RedirectToPage("/Index");
            }

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                Configs = await _hospitalServerConfigService.SearchConfigsAsync(SearchTerm);
            }
            else
            {
                Configs = await _hospitalServerConfigService.GetAllConfigsAsync();
            }

            if (EditId.HasValue)
            {
                var configToEdit = await _hospitalServerConfigService.GetByIdAsync(EditId.Value);
                if (configToEdit != null)
                {
                    Config = configToEdit;
                    // 不显示加密密码，只在编辑时提供密码字段
                    Config.EncryptedPassword = string.Empty;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 检查用户是否具有管理员角色
            if (!User.IsInRole("Admin") && User.Identity.Name != "admin")
            {
                TempData["ErrorMessage"] = "您没有权限执行此操作。";
                return RedirectToPage("/Index");
            }

            // 手动验证Config对象，而不依赖于整体ModelState
            var validationContext = new ValidationContext(Config, null, null);
            var validationResults = new List<ValidationResult>();
            bool isConfigValid = Validator.TryValidateObject(Config, validationContext, validationResults, true);
            
            // 如果Config对象验证失败，手动添加错误到ModelState
            if (!isConfigValid)
            {
                foreach (var validationResult in validationResults)
                {
                    if (validationResult.MemberNames.Any())
                    {
                        foreach (var memberName in validationResult.MemberNames)
                        {
                            ModelState.AddModelError($"Config.{memberName}", validationResult.ErrorMessage);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Config", validationResult.ErrorMessage);
                    }
                }
                
                // 保留用户输入的数据，不重新加载配置列表，以保持用户输入
                Configs = await _hospitalServerConfigService.GetAllConfigsAsync();
                return Page();
            }

            try
            {
                if (Config.Id > 0)
                {
                    // 更新现有配置
                    var updatedConfig = await _hospitalServerConfigService.UpdateAsync(Config);
                    TempData["SuccessMessage"] = "医院服务器配置已成功更新。";
                }
                else
                {
                    // 创建新配置
                    var createdConfig = await _hospitalServerConfigService.CreateAsync(Config);
                    TempData["SuccessMessage"] = "医院服务器配置已成功创建。";
                }
            
                return RedirectToPage();
            }
            catch (ArgumentException ex)
            {
                // 处理特定的参数异常，比如找不到要更新的配置
                ModelState.AddModelError("", $"操作失败: {ex.Message}");
                // 保留用户输入的数据，不重新加载配置列表，以保持用户输入
                Configs = await _hospitalServerConfigService.GetAllConfigsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                // 处理其他一般性异常
                ModelState.AddModelError("", $"保存失败: {ex.Message}");
                // 保留用户输入的数据，不重新加载配置列表，以保持用户输入
                Configs = await _hospitalServerConfigService.GetAllConfigsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            // 检查用户是否具有管理员角色
            if (!User.IsInRole("Admin") && User.Identity.Name != "admin")
            {
                TempData["ErrorMessage"] = "您没有权限执行此操作。";
                return RedirectToPage("/Index");
            }

            if (DeleteId.HasValue)
            {
                var success = await _hospitalServerConfigService.DeleteAsync(DeleteId.Value);
                if (success)
                {
                    TempData["SuccessMessage"] = "医院服务器配置已成功删除。";
                }
                else
                {
                    TempData["ErrorMessage"] = "删除失败，配置不存在。";
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            // 检查用户是否具有管理员角色
            if (!User.IsInRole("Admin") && User.Identity.Name != "admin")
            {
                TempData["ErrorMessage"] = "您没有权限执行此操作。";
                return RedirectToPage("/Index");
            }

            var success = await _hospitalServerConfigService.ToggleStatusAsync(id);
            if (success)
            {
                var config = await _hospitalServerConfigService.GetByIdAsync(id);
                var statusText = config?.IsActive == true ? "启用" : "禁用";
                TempData["SuccessMessage"] = $"医院服务器配置已{statusText}。";
            }
            else
            {
                TempData["ErrorMessage"] = "操作失败，配置不存在。";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            // 检查用户是否具有管理员角色
            if (!User.IsInRole("Admin") && User.Identity.Name != "admin")
            {
                TempData["ErrorMessage"] = "您没有权限执行此操作。";
                return RedirectToPage("/Index");
            }

            Configs = await _hospitalServerConfigService.SearchConfigsAsync(SearchTerm);
            return Page();
        }
    }
}