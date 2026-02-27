using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LisReportServer.Models;
using LisReportServer.Services;
using LisReportServer.Filters;
using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Pages.Admin
{
    [ValidateAntiForgeryToken]
    [LocalAdminOnly]  // 仅允许本地系统管理员访问
    public class HospitalProfilesModel : PageModel
    {
        private readonly IHospitalProfileService _profileService;
        private readonly ILogger<HospitalProfilesModel> _logger;

        public HospitalProfilesModel(
            IHospitalProfileService profileService,
            ILogger<HospitalProfilesModel> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        [BindProperty]
        public List<HospitalProfile> Profiles { get; set; } = new();

        [BindProperty]
        public HospitalProfile Profile { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DeleteId { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public bool KeepModalOpen { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    Profiles = await _profileService.SearchAsync(SearchTerm);
                }
                else
                {
                    Profiles = await _profileService.GetAllAsync();
                }

                if (EditId.HasValue)
                {
                    var profileToEdit = await _profileService.GetByIdAsync(EditId.Value);
                    if (profileToEdit != null)
                    {
                        Profile = profileToEdit;
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载医院配置列表时发生错误");
                ErrorMessage = "加载数据失败，请稍后重试。";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 手动验证Profile对象
            var validationContext = new ValidationContext(Profile, null, null);
            var validationResults = new List<ValidationResult>();
            bool isProfileValid = Validator.TryValidateObject(Profile, validationContext, validationResults, true);

            if (!isProfileValid)
            {
                foreach (var validationResult in validationResults)
                {
                    if (validationResult.MemberNames.Any())
                    {
                        foreach (var memberName in validationResult.MemberNames)
                        {
                            ModelState.AddModelError($"Profile.{memberName}", validationResult.ErrorMessage ?? "验证失败");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Profile", validationResult.ErrorMessage ?? "验证失败");
                    }
                }

                KeepModalOpen = true;
                Profiles = await _profileService.GetAllAsync();
                return Page();
            }

            try
            {
                if (Profile.Id > 0)
                {
                    // 更新现有配置
                    await _profileService.UpdateAsync(Profile);
                    SuccessMessage = "医院配置已成功更新。";
                }
                else
                {
                    // 创建新配置
                    await _profileService.CreateAsync(Profile);
                    SuccessMessage = "医院配置已成功创建。";
                }

                return RedirectToPage();
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", $"操作失败: {ex.Message}");
                KeepModalOpen = true;
                Profiles = await _profileService.GetAllAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存医院配置时发生错误");
                ModelState.AddModelError("", $"保存失败: {ex.Message}");
                KeepModalOpen = true;
                Profiles = await _profileService.GetAllAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                if (DeleteId.HasValue)
                {
                    var success = await _profileService.DeleteAsync(DeleteId.Value);
                    if (success)
                    {
                        SuccessMessage = "医院配置已成功删除。";
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
                _logger.LogError(ex, "删除医院配置时发生错误");
                ErrorMessage = $"删除失败: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            try
            {
                var success = await _profileService.ToggleStatusAsync(id);
                if (success)
                {
                    var profile = await _profileService.GetByIdAsync(id);
                    var statusText = profile?.IsActive == true ? "启用" : "禁用";
                    SuccessMessage = $"医院配置已{statusText}。";
                }
                else
                {
                    ErrorMessage = "操作失败，配置不存在。";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换医院配置状态时发生错误");
                ErrorMessage = $"操作失败: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            Profiles = await _profileService.SearchAsync(SearchTerm);
            return Page();
        }

        public async Task<IActionResult> OnGetGetProfileAsync(int id)
        {
            try
            {
                var profile = await _profileService.GetByIdAsync(id);
                if (profile == null)
                {
                    return new JsonResult(new { error = "配置不存在" }) { StatusCode = 404 };
                }

                return new JsonResult(new
                {
                    id = profile.Id,
                    hospitalName = profile.HospitalName,
                    hospitalCode = profile.HospitalCode,
                    shortName = profile.ShortName,
                    address = profile.Address,
                    contactPhone = profile.ContactPhone,
                    contactEmail = profile.ContactEmail,
                    description = profile.Description,
                    logo = profile.Logo,
                    isActive = profile.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医院配置数据时发生错误，ID: {Id}", id);
                return new JsonResult(new { error = "获取数据失败" }) { StatusCode = 500 };
            }
        }
    }
}
