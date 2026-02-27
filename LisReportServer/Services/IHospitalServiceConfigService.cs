using LisReportServer.Models;

namespace LisReportServer.Services
{
    /// <summary>
    /// 医院服务配置服务接口
    /// </summary>
    public interface IHospitalServiceConfigService
    {
        /// <summary>
        /// 获取所有服务配置
        /// </summary>
        Task<List<HospitalServiceConfig>> GetAllAsync();

        /// <summary>
        /// 根据医院ID获取服务配置
        /// </summary>
        Task<List<HospitalServiceConfig>> GetByHospitalIdAsync(int hospitalId);

        /// <summary>
        /// 根据ID获取服务配置
        /// </summary>
        Task<HospitalServiceConfig?> GetByIdAsync(int id);

        /// <summary>
        /// 根据服务发现键值获取服务配置
        /// </summary>
        Task<HospitalServiceConfig?> GetByDiscoveryKeyAsync(int hospitalId, string discoveryKey);

        /// <summary>
        /// 根据服务类别获取服务配置
        /// </summary>
        Task<List<HospitalServiceConfig>> GetByCategoryAsync(string category);

        /// <summary>
        /// 创建服务配置
        /// </summary>
        Task<HospitalServiceConfig> CreateAsync(HospitalServiceConfig serviceConfig);

        /// <summary>
        /// 更新服务配置
        /// </summary>
        Task<HospitalServiceConfig> UpdateAsync(HospitalServiceConfig serviceConfig);

        /// <summary>
        /// 删除服务配置
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 批量删除服务配置（根据医院ID）
        /// </summary>
        Task<int> DeleteByHospitalIdAsync(int hospitalId);

        /// <summary>
        /// 切换服务状态
        /// </summary>
        Task<bool> ToggleStatusAsync(int id);

        /// <summary>
        /// 搜索服务配置
        /// </summary>
        Task<List<HospitalServiceConfig>> SearchAsync(string searchTerm);

        /// <summary>
        /// 检查服务发现键值是否已存在
        /// </summary>
        Task<bool> DiscoveryKeyExistsAsync(int hospitalId, string discoveryKey, int? excludeId = null);

        /// <summary>
        /// 测试服务连接
        /// </summary>
        Task<bool> TestConnectionAsync(int id);

        /// <summary>
        /// 获取服务健康状态
        /// </summary>
        Task<Dictionary<int, bool>> GetHealthStatusAsync(int hospitalId);
    }
}
