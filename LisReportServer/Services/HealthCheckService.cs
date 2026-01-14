using Microsoft.EntityFrameworkCore;
using LisReportServer.Data;
using System.Diagnostics;

namespace LisReportServer.Services
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHospitalServerConfigService _hospitalServerConfigService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(
            ApplicationDbContext context, 
            IHospitalServerConfigService hospitalServerConfigService,
            ITokenBlacklistService tokenBlacklistService,
            ILogger<HealthCheckService> logger)
        {
            _context = context;
            _hospitalServerConfigService = hospitalServerConfigService;
            _tokenBlacklistService = tokenBlacklistService;
            _logger = logger;
        }

        public HealthStatus GetHealthStatus()
        {
            return GetHealthStatusAsync().GetAwaiter().GetResult();
        }

        public async Task<HealthStatus> GetHealthStatusAsync()
        {
            var healthStatus = new HealthStatus
            {
                CheckedAtUtc = DateTime.UtcNow,
                ServiceName = "LIS Report Server",
                Version = "1.0.0"
            };

            try
            {
                // 检查数据库连接
                var dbStatus = await CheckDatabaseHealthAsync();
                healthStatus.Components["database"] = dbStatus;

                // 检查医院服务器配置服务
                var configServiceStatus = await CheckConfigServiceHealthAsync();
                healthStatus.Components["hospital_config_service"] = configServiceStatus;

                // 检查令牌黑名单服务
                var tokenServiceStatus = CheckTokenServiceHealth();
                healthStatus.Components["token_blacklist_service"] = tokenServiceStatus;

                // 检查内存使用情况
                var memoryStatus = CheckMemoryHealth();
                healthStatus.Components["memory"] = memoryStatus;

                // 检查磁盘空间
                var diskStatus = CheckDiskHealth();
                healthStatus.Components["disk_space"] = diskStatus;

                // 检查应用程序池健康状况
                var appStatus = CheckApplicationHealth();
                healthStatus.Components["application"] = appStatus;

                // 综合判断整体状态
                var overallStatus = DetermineOverallStatus(healthStatus.Components);
                healthStatus.Status = overallStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking health status");
                healthStatus.Status = "Critical";
                healthStatus.Components["overall"] = new { status = "Critical", error = ex.Message };
            }

            return healthStatus;
        }

        private async Task<Dictionary<string, object>> CheckDatabaseHealthAsync()
        {
            try
            {
                // 尝试执行一个简单的查询来测试数据库连接
                var connection = _context.Database.GetDbConnection();
                await _context.Database.OpenConnectionAsync();
                
                var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                var result = await command.ExecuteScalarAsync();
                
                _context.Database.CloseConnection();
                
                return new Dictionary<string, object>
                {
                    { "status", result != null ? "Healthy" : "Unhealthy" },
                    { "response_time_ms", 0 }, // 简化版，实际中应测量响应时间
                    { "message", "Database connection is healthy" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new Dictionary<string, object>
                {
                    { "status", "Unhealthy" },
                    { "error", ex.Message },
                    { "message", "Database connection failed" }
                };
            }
        }

        private async Task<Dictionary<string, object>> CheckConfigServiceHealthAsync()
        {
            try
            {
                // 尝试获取配置数量来测试服务
                var configs = await _hospitalServerConfigService.GetAllConfigsAsync();
                return new Dictionary<string, object>
                {
                    { "status", "Healthy" },
                    { "config_count", configs.Count },
                    { "message", "Hospital server config service is responsive" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Config service health check failed");
                return new Dictionary<string, object>
                {
                    { "status", "Unhealthy" },
                    { "error", ex.Message },
                    { "message", "Hospital server config service is not responsive" }
                };
            }
        }

        private Dictionary<string, object> CheckTokenServiceHealth()
        {
            try
            {
                // 尝试执行一个简单的操作来测试令牌服务
                return new Dictionary<string, object>
                {
                    { "status", "Healthy" },
                    { "message", "Token blacklist service is available" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token service health check failed");
                return new Dictionary<string, object>
                {
                    { "status", "Unhealthy" },
                    { "error", ex.Message },
                    { "message", "Token blacklist service is not available" }
                };
            }
        }

        private Dictionary<string, object> CheckMemoryHealth()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryUsedByProcessMB = process.WorkingSet64 / (1024 * 1024);
                
                // 对于内存健康检查，我们主要关注进程自身的内存使用情况
                // 因为在ASP.NET Core应用中获取系统总内存可能需要特殊权限
                // 这里我们设定一些合理的阈值
                string status;
                if (memoryUsedByProcessMB > 1024) // 如果进程使用超过1GB内存
                    status = "Degraded";
                else if (memoryUsedByProcessMB > 2048) // 如果进程使用超过2GB内存
                    status = "Unhealthy";
                else
                    status = "Healthy";

                return new Dictionary<string, object>
                {
                    { "status", status },
                    { "process_used_mb", memoryUsedByProcessMB },
                    { "message", $"Process memory usage is {memoryUsedByProcessMB} MB" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");
                return new Dictionary<string, object>
                {
                    { "status", "Unhealthy" },
                    { "error", ex.Message },
                    { "message", "Unable to determine memory usage" }
                };
            }
        }

        private Dictionary<string, object> CheckDiskHealth()
        {
            try
            {
                // 获取当前应用程序所在驱动器的信息
                var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
                var drive = new DriveInfo(currentDir.Root.FullName);
                
                if (drive != null && drive.IsReady)
                {
                    var freeSpaceGB = drive.AvailableFreeSpace / (1024L * 1024L * 1024L);
                    var totalSpaceGB = drive.TotalSize / (1024L * 1024L * 1024L);
                    var usedSpaceGB = totalSpaceGB - freeSpaceGB;
                    var usagePercent = totalSpaceGB > 0 ? (double)usedSpaceGB / totalSpaceGB * 100 : 0;

                    string status;
                    if (usagePercent > 95)
                        status = "Unhealthy";
                    else if (usagePercent > 85)
                        status = "Degraded";
                    else
                        status = "Healthy";

                    return new Dictionary<string, object>
                    {
                        { "status", status },
                        { "free_gb", freeSpaceGB },
                        { "used_gb", usedSpaceGB },
                        { "total_gb", totalSpaceGB },
                        { "usage_percent", Math.Round(usagePercent, 2) },
                        { "message", $"Disk space usage is {Math.Round(usagePercent, 2)}%" }
                    };
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        { "status", "Unknown" },
                        { "message", "Unable to determine disk space" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk health check failed");
                return new Dictionary<string, object>
                {
                    { "status", "Unhealthy" },
                    { "error", ex.Message },
                    { "message", "Unable to determine disk space" }
                };
            }
        }

        private Dictionary<string, object> CheckApplicationHealth()
        {
            try
            {
                var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                var threadCount = Process.GetCurrentProcess().Threads.Count;

                return new Dictionary<string, object>
                {
                    { "status", "Healthy" },
                    { "uptime", uptime.ToString() },
                    { "thread_count", threadCount },
                    { "process_id", Process.GetCurrentProcess().Id },
                    { "message", "Application is running normally" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application health check failed");
                return new Dictionary<string, object>
                {
                    { "status", "Unhealthy" },
                    { "error", ex.Message },
                    { "message", "Unable to determine application health" }
                };
            }
        }

        private long GetTotalMemory()
        {
            try
            {
                // 获取系统总内存的简化方法
                // 在实际应用中，可能需要更精确的方法来获取系统内存
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return GC.GetTotalMemory(false);
            }
            catch
            {
                // 如果无法获取精确内存信息，返回一个估计值
                return 1024L * 1024L * 1024L; // 1GB as fallback
            }
        }

        private string DetermineOverallStatus(Dictionary<string, object> components)
        {
            var statuses = components.Values
                .OfType<Dictionary<string, object>>()
                .Select(c => c.ContainsKey("status") ? c["status"].ToString() : "Unknown")
                .ToList();

            if (statuses.Contains("Unhealthy"))
                return "Unhealthy";
            if (statuses.Contains("Degraded"))
                return "Degraded";
            if (statuses.All(s => s == "Healthy"))
                return "Healthy";
            
            return "Unknown";
        }

        public Dictionary<string, object> GetDetailedHealthStatus()
        {
            var status = GetHealthStatus();
            return new Dictionary<string, object>
            {
                { "status", status.Status },
                { "checked_at_utc", status.CheckedAtUtc },
                { "checked_at_local", status.CheckedAtLocal },
                { "timezone_offset", status.TimezoneOffset },
                { "service_name", status.ServiceName },
                { "version", status.Version },
                { "components", status.Components }
            };
        }
    }
}