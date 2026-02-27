using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using LisReportServer.Models;
using LisReportServer.Services;
using LisReportServer.Filters;
using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Pages.Admin
{
    [ValidateAntiForgeryToken]
    [LocalAdminOnly]  // 仅允许本地系统管理员访问
    public class HospitalServiceConfigsModel : PageModel
    {
        private readonly IHospitalServiceConfigService _serviceConfigService;
        private readonly IHospitalProfileService _profileService;
        private readonly ILogger<HospitalServiceConfigsModel> _logger;

        public HospitalServiceConfigsModel(
            IHospitalServiceConfigService serviceConfigService,
            IHospitalProfileService profileService,
            ILogger<HospitalServiceConfigsModel> logger)
        {
            _serviceConfigService = serviceConfigService;
            _profileService = profileService;
            _logger = logger;
        }

        [BindProperty]
        public List<HospitalServiceConfig> ServiceConfigs { get; set; } = new();

        [BindProperty]
        public HospitalServiceConfig ServiceConfig { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? HospitalId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteId { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public bool KeepModalOpen { get; set; }

        public List<HospitalProfile> Hospitals { get; set; } = new();
        public List<string> ServiceCategories { get; set; } = new();
        public List<string> AuthTypes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // 加载医院列表
                Hospitals = await _profileService.GetAllAsync();
                ServiceCategories = ServiceCategory.GetAll();
                AuthTypes = AuthType.GetAll();

                // 根据条件加载服务配置列表
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    ServiceConfigs = await _serviceConfigService.SearchAsync(SearchTerm);
                }
                else if (HospitalId.HasValue)
                {
                    ServiceConfigs = await _serviceConfigService.GetByHospitalIdAsync(HospitalId.Value);
                }
                else if (!string.IsNullOrEmpty(Category))
                {
                    ServiceConfigs = await _serviceConfigService.GetByCategoryAsync(Category);
                }
                else
                {
                    ServiceConfigs = await _serviceConfigService.GetAllAsync();
                }

                if (EditId.HasValue)
                {
                    var configToEdit = await _serviceConfigService.GetByIdAsync(EditId.Value);
                    if (configToEdit != null)
                    {
                        ServiceConfig = configToEdit;
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载服务配置列表时发生错误");
                ErrorMessage = "加载数据失败，请稍后重试。";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 手动验证ServiceConfig对象
            var validationContext = new ValidationContext(ServiceConfig, null, null);
            var validationResults = new List<ValidationResult>();
            bool isConfigValid = Validator.TryValidateObject(ServiceConfig, validationContext, validationResults, true);

            if (!isConfigValid)
            {
                foreach (var validationResult in validationResults)
                {
                    if (validationResult.MemberNames.Any())
                    {
                        foreach (var memberName in validationResult.MemberNames)
                        {
                            ModelState.AddModelError($"ServiceConfig.{memberName}", validationResult.ErrorMessage ?? "验证失败");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ServiceConfig", validationResult.ErrorMessage ?? "验证失败");
                    }
                }

                KeepModalOpen = true;
                await LoadDropdownDataAsync();
                ServiceConfigs = await _serviceConfigService.GetAllAsync();
                return Page();
            }

            try
            {
                if (ServiceConfig.Id > 0)
                {
                    // 更新现有配置
                    await _serviceConfigService.UpdateAsync(ServiceConfig);
                    SuccessMessage = "服务配置已成功更新。";
                }
                else
                {
                    // 创建新配置
                    await _serviceConfigService.CreateAsync(ServiceConfig);
                    SuccessMessage = "服务配置已成功创建。";
                }

                return RedirectToPage();
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", $"操作失败: {ex.Message}");
                KeepModalOpen = true;
                await LoadDropdownDataAsync();
                ServiceConfigs = await _serviceConfigService.GetAllAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存服务配置时发生错误");
                ModelState.AddModelError("", $"保存失败: {ex.Message}");
                KeepModalOpen = true;
                await LoadDropdownDataAsync();
                ServiceConfigs = await _serviceConfigService.GetAllAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                if (DeleteId.HasValue)
                {
                    var success = await _serviceConfigService.DeleteAsync(DeleteId.Value);
                    if (success)
                    {
                        SuccessMessage = "服务配置已成功删除。";
                    }
                    else
                    {
                        ErrorMessage = "删除失败，配置不存在。";
                    }
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除服务配置时发生错误");
                ErrorMessage = $"删除失败: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            try
            {
                var success = await _serviceConfigService.ToggleStatusAsync(id);
                if (success)
                {
                    var config = await _serviceConfigService.GetByIdAsync(id);
                    var statusText = config?.IsActive == true ? "启用" : "禁用";
                    SuccessMessage = $"服务配置已{statusText}。";
                }
                else
                {
                    ErrorMessage = "操作失败，配置不存在。";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换服务配置状态时发生错误");
                ErrorMessage = $"操作失败: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostTestConnectionAsync(int id)
        {
            try
            {
                var result = await _serviceConfigService.TestConnectionAsync(id);
                if (result)
                {
                    SuccessMessage = "服务连接测试成功！";
                }
                else
                {
                    ErrorMessage = "服务连接测试失败，请检查配置。";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试服务连接时发生错误");
                ErrorMessage = $"连接测试失败: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetGetServiceConfigAsync(int id)
        {
            try
            {
                var config = await _serviceConfigService.GetByIdAsync(id);
                if (config == null)
                {
                    return new JsonResult(new { error = "配置不存在" }) { StatusCode = 404 };
                }

                return new JsonResult(new
                {
                    id = config.Id,
                    hospitalProfileId = config.HospitalProfileId,
                    serviceName = config.ServiceName,
                    serviceCategory = config.ServiceCategory,
                    serviceDiscoveryKey = config.ServiceDiscoveryKey,
                    serviceAddress = config.ServiceAddress,
                    servicePort = config.ServicePort,
                    apiVersion = config.ApiVersion,
                    authType = config.AuthType,
                    username = config.Username,
                    encryptedPassword = config.EncryptedPassword,
                    apiKey = config.ApiKey,
                    timeout = config.Timeout,
                    retryCount = config.RetryCount,
                    healthCheckUrl = config.HealthCheckUrl,
                    isActive = config.IsActive,
                    priority = config.Priority,
                    description = config.Description
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取服务配置数据时发生错误，ID: {Id}", id);
                return new JsonResult(new { error = "获取数据失败" }) { StatusCode = 500 };
            }
        }

        private async Task LoadDropdownDataAsync()
        {
            Hospitals = await _profileService.GetAllAsync();
            ServiceCategories = ServiceCategory.GetAll();
            AuthTypes = AuthType.GetAll();
        }
    }
}
