using LisReportServer.Data;
using LisReportServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LisReportServer.Services
{
    /// <summary>
    /// 医院服务配置服务实现
    /// </summary>
    public class HospitalServiceConfigService : IHospitalServiceConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HospitalServiceConfigService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public HospitalServiceConfigService(
            ApplicationDbContext context,
            ILogger<HospitalServiceConfigService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<HospitalServiceConfig>> GetAllAsync()
        {
            try
            {
                return await _context.HospitalServiceConfigs
                    .Include(s => s.HospitalProfile)
                    .OrderBy(s => s.Priority)
                    .ThenByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有服务配置时发生错误");
                throw;
            }
        }

        public async Task<List<HospitalServiceConfig>> GetByHospitalIdAsync(int hospitalId)
        {
            try
            {
                return await _context.HospitalServiceConfigs
                    .Include(s => s.HospitalProfile)
                    .Where(s => s.HospitalProfileId == hospitalId)
                    .OrderBy(s => s.Priority)
                    .ThenByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医院ID获取服务配置时发生错误，医院ID: {HospitalId}", hospitalId);
                throw;
            }
        }

        public async Task<HospitalServiceConfig?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.HospitalServiceConfigs
                    .Include(s => s.HospitalProfile)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取服务配置时发生错误，ID: {Id}", id);
                throw;
            }
        }

        public async Task<HospitalServiceConfig?> GetByDiscoveryKeyAsync(int hospitalId, string discoveryKey)
        {
            try
            {
                return await _context.HospitalServiceConfigs
                    .Include(s => s.HospitalProfile)
                    .FirstOrDefaultAsync(s => s.HospitalProfileId == hospitalId && s.ServiceDiscoveryKey == discoveryKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据服务发现键值获取服务配置时发生错误，医院ID: {HospitalId}, 键值: {Key}", hospitalId, discoveryKey);
                throw;
            }
        }

        public async Task<List<HospitalServiceConfig>> GetByCategoryAsync(string category)
        {
            try
            {
                return await _context.HospitalServiceConfigs
                    .Include(s => s.HospitalProfile)
                    .Where(s => s.ServiceCategory == category)
                    .OrderBy(s => s.Priority)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据服务类别获取服务配置时发生错误，类别: {Category}", category);
                throw;
            }
        }

        public async Task<HospitalServiceConfig> CreateAsync(HospitalServiceConfig serviceConfig)
        {
            try
            {
                // 检查医院是否存在
                var hospital = await _context.HospitalProfiles.FindAsync(serviceConfig.HospitalProfileId);
                if (hospital == null)
                {
                    throw new ArgumentException($"未找到ID为 {serviceConfig.HospitalProfileId} 的医院配置");
                }

                // 检查服务发现键值是否已存在
                if (await DiscoveryKeyExistsAsync(serviceConfig.HospitalProfileId, serviceConfig.ServiceDiscoveryKey))
                {
                    throw new ArgumentException($"服务发现键值 '{serviceConfig.ServiceDiscoveryKey}' 在该医院下已存在");
                }

                serviceConfig.CreatedAt = DateTime.UtcNow;
                serviceConfig.UpdatedAt = DateTime.UtcNow;

                _context.HospitalServiceConfigs.Add(serviceConfig);
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功创建服务配置，ID: {Id}, 名称: {Name}", serviceConfig.Id, serviceConfig.ServiceName);
                return serviceConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建服务配置时发生错误");
                throw;
            }
        }

        public async Task<HospitalServiceConfig> UpdateAsync(HospitalServiceConfig serviceConfig)
        {
            try
            {
                var existingConfig = await _context.HospitalServiceConfigs.FindAsync(serviceConfig.Id);
                if (existingConfig == null)
                {
                    throw new ArgumentException($"未找到ID为 {serviceConfig.Id} 的服务配置");
                }

                // 检查服务发现键值是否与其他服务重复
                if (await DiscoveryKeyExistsAsync(serviceConfig.HospitalProfileId, serviceConfig.ServiceDiscoveryKey, serviceConfig.Id))
                {
                    throw new ArgumentException($"服务发现键值 '{serviceConfig.ServiceDiscoveryKey}' 已被该医院下的其他服务使用");
                }

                // 更新属性
                existingConfig.ServiceName = serviceConfig.ServiceName;
                existingConfig.ServiceCategory = serviceConfig.ServiceCategory;
                existingConfig.ServiceDiscoveryKey = serviceConfig.ServiceDiscoveryKey;
                existingConfig.ServiceAddress = serviceConfig.ServiceAddress;
                existingConfig.ServicePort = serviceConfig.ServicePort;
                existingConfig.ApiVersion = serviceConfig.ApiVersion;
                existingConfig.AuthType = serviceConfig.AuthType;
                existingConfig.Username = serviceConfig.Username;
                existingConfig.EncryptedPassword = serviceConfig.EncryptedPassword;
                existingConfig.ApiKey = serviceConfig.ApiKey;
                existingConfig.Timeout = serviceConfig.Timeout;
                existingConfig.RetryCount = serviceConfig.RetryCount;
                existingConfig.HealthCheckUrl = serviceConfig.HealthCheckUrl;
                existingConfig.IsActive = serviceConfig.IsActive;
                existingConfig.Priority = serviceConfig.Priority;
                existingConfig.Description = serviceConfig.Description;
                existingConfig.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("成功更新服务配置，ID: {Id}, 名称: {Name}", serviceConfig.Id, serviceConfig.ServiceName);
                return existingConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新服务配置时发生错误，ID: {Id}", serviceConfig.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var config = await _context.HospitalServiceConfigs.FindAsync(id);
                if (config == null)
                {
                    _logger.LogWarning("尝试删除不存在的服务配置，ID: {Id}", id);
                    return false;
                }

                _context.HospitalServiceConfigs.Remove(config);
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功删除服务配置，ID: {Id}, 名称: {Name}", id, config.ServiceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除服务配置时发生错误，ID: {Id}", id);
                throw;
            }
        }

        public async Task<int> DeleteByHospitalIdAsync(int hospitalId)
        {
            try
            {
                var configs = await _context.HospitalServiceConfigs
                    .Where(s => s.HospitalProfileId == hospitalId)
                    .ToListAsync();

                if (!configs.Any())
                {
                    return 0;
                }

                _context.HospitalServiceConfigs.RemoveRange(configs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功删除医院 {HospitalId} 的 {Count} 个服务配置", hospitalId, configs.Count);
                return configs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除服务配置时发生错误，医院ID: {HospitalId}", hospitalId);
                throw;
            }
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            try
            {
                var config = await _context.HospitalServiceConfigs.FindAsync(id);
                if (config == null)
                {
                    _logger.LogWarning("尝试切换不存在的服务配置状态，ID: {Id}", id);
                    return false;
                }

                config.IsActive = !config.IsActive;
                config.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功切换服务配置状态，ID: {Id}, 新状态: {Status}", id, config.IsActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换服务配置状态时发生错误，ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<HospitalServiceConfig>> SearchAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllAsync();
                }

                searchTerm = searchTerm.Trim().ToLower();

                return await _context.HospitalServiceConfigs
                    .Include(s => s.HospitalProfile)
                    .Where(s =>
                        s.ServiceName.ToLower().Contains(searchTerm) ||
                        s.ServiceCategory.ToLower().Contains(searchTerm) ||
                        s.ServiceDiscoveryKey.ToLower().Contains(searchTerm) ||
                        s.ServiceAddress.ToLower().Contains(searchTerm))
                    .OrderBy(s => s.Priority)
                    .ThenByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索服务配置时发生错误，搜索词: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> DiscoveryKeyExistsAsync(int hospitalId, string discoveryKey, int? excludeId = null)
        {
            try
            {
                var query = _context.HospitalServiceConfigs
                    .Where(s => s.HospitalProfileId == hospitalId && s.ServiceDiscoveryKey == discoveryKey);

                if (excludeId.HasValue)
                {
                    query = query.Where(s => s.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查服务发现键值是否存在时发生错误，医院ID: {HospitalId}, 键值: {Key}", hospitalId, discoveryKey);
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync(int id)
        {
            try
            {
                var config = await GetByIdAsync(id);
                if (config == null)
                {
                    _logger.LogWarning("测试连接失败：服务配置不存在，ID: {Id}", id);
                    return false;
                }

                if (!config.IsActive)
                {
                    _logger.LogWarning("测试连接失败：服务配置未启用，ID: {Id}", id);
                    return false;
                }

                var testUrl = string.IsNullOrWhiteSpace(config.HealthCheckUrl)
                    ? config.ServiceAddress
                    : config.HealthCheckUrl;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(config.Timeout ?? 30);

                var response = await httpClient.GetAsync(testUrl);
                var isSuccess = response.IsSuccessStatusCode;

                _logger.LogInformation("服务连接测试完成，ID: {Id}, 结果: {Result}", id, isSuccess ? "成功" : "失败");
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试服务连接时发生错误，ID: {Id}", id);
                return false;
            }
        }

        public async Task<Dictionary<int, bool>> GetHealthStatusAsync(int hospitalId)
        {
            try
            {
                var configs = await GetByHospitalIdAsync(hospitalId);
                var healthStatus = new Dictionary<int, bool>();

                foreach (var config in configs.Where(c => c.IsActive))
                {
                    var isHealthy = await TestConnectionAsync(config.Id);
                    healthStatus[config.Id] = isHealthy;
                }

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取服务健康状态时发生错误，医院ID: {HospitalId}", hospitalId);
                throw;
            }
        }
    }
}
