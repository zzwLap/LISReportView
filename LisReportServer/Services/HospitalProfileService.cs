using LisReportServer.Data;
using LisReportServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LisReportServer.Services
{
    /// <summary>
    /// 医院基本信息配置服务实现
    /// </summary>
    public class HospitalProfileService : IHospitalProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HospitalProfileService> _logger;

        public HospitalProfileService(
            ApplicationDbContext context,
            ILogger<HospitalProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<HospitalProfile>> GetAllAsync()
        {
            try
            {
                return await _context.HospitalProfiles
                    .Include(h => h.ServiceConfigs)
                    .OrderByDescending(h => h.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有医院配置时发生错误");
                throw;
            }
        }

        public async Task<HospitalProfile?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.HospitalProfiles
                    .Include(h => h.ServiceConfigs)
                    .FirstOrDefaultAsync(h => h.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取医院配置时发生错误，ID: {Id}", id);
                throw;
            }
        }

        public async Task<HospitalProfile?> GetByCodeAsync(string hospitalCode)
        {
            try
            {
                return await _context.HospitalProfiles
                    .Include(h => h.ServiceConfigs)
                    .FirstOrDefaultAsync(h => h.HospitalCode == hospitalCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据编码获取医院配置时发生错误，编码: {Code}", hospitalCode);
                throw;
            }
        }

        public async Task<HospitalProfile?> GetByNameAsync(string hospitalName)
        {
            try
            {
                return await _context.HospitalProfiles
                    .Include(h => h.ServiceConfigs)
                    .FirstOrDefaultAsync(h => h.HospitalName == hospitalName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据名称获取医院配置时发生错误，名称: {Name}", hospitalName);
                throw;
            }
        }

        public async Task<HospitalProfile> CreateAsync(HospitalProfile profile)
        {
            try
            {
                // 检查医院编码是否已存在
                if (await CodeExistsAsync(profile.HospitalCode))
                {
                    throw new ArgumentException($"医院编码 '{profile.HospitalCode}' 已存在");
                }

                // 检查医院名称是否已存在
                if (await NameExistsAsync(profile.HospitalName))
                {
                    throw new ArgumentException($"医院名称 '{profile.HospitalName}' 已存在");
                }

                profile.CreatedAt = DateTime.UtcNow;
                profile.UpdatedAt = DateTime.UtcNow;

                _context.HospitalProfiles.Add(profile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功创建医院配置，ID: {Id}, 名称: {Name}", profile.Id, profile.HospitalName);
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医院配置时发生错误");
                throw;
            }
        }

        public async Task<HospitalProfile> UpdateAsync(HospitalProfile profile)
        {
            try
            {
                var existingProfile = await _context.HospitalProfiles.FindAsync(profile.Id);
                if (existingProfile == null)
                {
                    throw new ArgumentException($"未找到ID为 {profile.Id} 的医院配置");
                }

                // 检查医院编码是否与其他医院重复
                if (await CodeExistsAsync(profile.HospitalCode, profile.Id))
                {
                    throw new ArgumentException($"医院编码 '{profile.HospitalCode}' 已被其他医院使用");
                }

                // 检查医院名称是否与其他医院重复
                if (await NameExistsAsync(profile.HospitalName, profile.Id))
                {
                    throw new ArgumentException($"医院名称 '{profile.HospitalName}' 已被其他医院使用");
                }

                // 更新属性
                existingProfile.HospitalName = profile.HospitalName;
                existingProfile.HospitalCode = profile.HospitalCode;
                existingProfile.ShortName = profile.ShortName;
                existingProfile.Address = profile.Address;
                existingProfile.ContactPhone = profile.ContactPhone;
                existingProfile.ContactEmail = profile.ContactEmail;
                existingProfile.Description = profile.Description;
                existingProfile.Logo = profile.Logo;
                existingProfile.IsActive = profile.IsActive;
                existingProfile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("成功更新医院配置，ID: {Id}, 名称: {Name}", profile.Id, profile.HospitalName);
                return existingProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医院配置时发生错误，ID: {Id}", profile.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var profile = await _context.HospitalProfiles.FindAsync(id);
                if (profile == null)
                {
                    _logger.LogWarning("尝试删除不存在的医院配置，ID: {Id}", id);
                    return false;
                }

                _context.HospitalProfiles.Remove(profile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功删除医院配置，ID: {Id}, 名称: {Name}", id, profile.HospitalName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医院配置时发生错误，ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            try
            {
                var profile = await _context.HospitalProfiles.FindAsync(id);
                if (profile == null)
                {
                    _logger.LogWarning("尝试切换不存在的医院配置状态，ID: {Id}", id);
                    return false;
                }

                profile.IsActive = !profile.IsActive;
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功切换医院配置状态，ID: {Id}, 新状态: {Status}", id, profile.IsActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换医院配置状态时发生错误，ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<HospitalProfile>> SearchAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllAsync();
                }

                searchTerm = searchTerm.Trim().ToLower();

                return await _context.HospitalProfiles
                    .Include(h => h.ServiceConfigs)
                    .Where(h =>
                        h.HospitalName.ToLower().Contains(searchTerm) ||
                        h.HospitalCode.ToLower().Contains(searchTerm) ||
                        (h.ShortName != null && h.ShortName.ToLower().Contains(searchTerm)) ||
                        (h.Address != null && h.Address.ToLower().Contains(searchTerm)))
                    .OrderByDescending(h => h.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医院配置时发生错误，搜索词: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> CodeExistsAsync(string hospitalCode, int? excludeId = null)
        {
            try
            {
                var query = _context.HospitalProfiles
                    .Where(h => h.HospitalCode == hospitalCode);

                if (excludeId.HasValue)
                {
                    query = query.Where(h => h.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查医院编码是否存在时发生错误，编码: {Code}", hospitalCode);
                throw;
            }
        }

        public async Task<bool> NameExistsAsync(string hospitalName, int? excludeId = null)
        {
            try
            {
                var query = _context.HospitalProfiles
                    .Where(h => h.HospitalName == hospitalName);

                if (excludeId.HasValue)
                {
                    query = query.Where(h => h.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查医院名称是否存在时发生错误，名称: {Name}", hospitalName);
                throw;
            }
        }
    }
}
