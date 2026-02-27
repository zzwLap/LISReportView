using LisReportServer.Models;

namespace LisReportServer.Services
{
    /// <summary>
    /// 医院基本信息配置服务接口
    /// </summary>
    public interface IHospitalProfileService
    {
        /// <summary>
        /// 获取所有医院配置
        /// </summary>
        Task<List<HospitalProfile>> GetAllAsync();

        /// <summary>
        /// 根据ID获取医院配置
        /// </summary>
        Task<HospitalProfile?> GetByIdAsync(int id);

        /// <summary>
        /// 根据医院编码获取医院配置
        /// </summary>
        Task<HospitalProfile?> GetByCodeAsync(string hospitalCode);

        /// <summary>
        /// 根据医院名称获取医院配置
        /// </summary>
        Task<HospitalProfile?> GetByNameAsync(string hospitalName);

        /// <summary>
        /// 创建医院配置
        /// </summary>
        Task<HospitalProfile> CreateAsync(HospitalProfile profile);

        /// <summary>
        /// 更新医院配置
        /// </summary>
        Task<HospitalProfile> UpdateAsync(HospitalProfile profile);

        /// <summary>
        /// 删除医院配置
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 切换医院状态
        /// </summary>
        Task<bool> ToggleStatusAsync(int id);

        /// <summary>
        /// 搜索医院配置
        /// </summary>
        Task<List<HospitalProfile>> SearchAsync(string searchTerm);

        /// <summary>
        /// 检查医院编码是否已存在
        /// </summary>
        Task<bool> CodeExistsAsync(string hospitalCode, int? excludeId = null);

        /// <summary>
        /// 检查医院名称是否已存在
        /// </summary>
        Task<bool> NameExistsAsync(string hospitalName, int? excludeId = null);
    }
}
