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
        public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime CheckedAtLocal { get; set; }
        public string TimezoneOffset { get; set; } = "UTC";
        public Dictionary<string, object> Components { get; set; } = new Dictionary<string, object>();
        public string ServiceName { get; set; } = "LIS Report Server";
        public string Version { get; set; } = "1.0.0";
        
        // 构造函数中设置本地时间
        public HealthStatus()
        {
            CheckedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(CheckedAtUtc, TimeZoneInfo.Local);
            TimezoneOffset = GetTimezoneOffsetString(TimeZoneInfo.Local);
        }
        
        // 用于设置客户端时区时间的方法
        public void SetClientTimezoneInfo(TimeZoneInfo clientTimezoneInfo)
        {
            CheckedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(CheckedAtUtc, clientTimezoneInfo);
            TimezoneOffset = GetTimezoneOffsetString(clientTimezoneInfo);
        }
        
        private static string GetTimezoneOffsetString(TimeZoneInfo timeZone)
        {
            var offset = timeZone.BaseUtcOffset;
            var sign = offset >= TimeSpan.Zero ? "+" : "-";
            var hours = Math.Abs(offset.Hours);
            var minutes = Math.Abs(offset.Minutes);
            return $"{sign}{hours:D2}:{minutes:D2}";
        }
    }
}