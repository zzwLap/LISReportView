using System.Text.Json;

namespace LisReportServer.Services
{
    public interface IHealthCheckService
    {
        HealthStatus GetHealthStatus();
        Task<HealthStatus> GetHealthStatusAsync();
        Dictionary<string, object> GetDetailedHealthStatus();
    }

    public class HealthStatus
    {
        public string Status { get; set; } = "Unknown";
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Components { get; set; } = new Dictionary<string, object>();
        public string ServiceName { get; set; } = "LIS Report Server";
        public string Version { get; set; } = "1.0.0";
    }
}