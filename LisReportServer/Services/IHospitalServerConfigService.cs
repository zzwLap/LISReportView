using LisReportServer.Models;

namespace LisReportServer.Services
{
    public interface IHospitalServerConfigService
    {
        Task<List<HospitalServerConfig>> GetAllConfigsAsync();
        Task<HospitalServerConfig?> GetByIdAsync(int id);
        Task<HospitalServerConfig> CreateAsync(HospitalServerConfig config);
        Task<HospitalServerConfig> UpdateAsync(HospitalServerConfig config);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleStatusAsync(int id);
        Task<List<HospitalServerConfig>> SearchConfigsAsync(string searchTerm);
    }
}