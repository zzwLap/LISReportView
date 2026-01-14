using LisReportServer.Services;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace LisReportServer.Services
{
    public class HealthStatusPublishingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthStatusPublishingService> _logger;
        private readonly TimeSpan _publishInterval;

        public HealthStatusPublishingService(IServiceProvider serviceProvider, ILogger<HealthStatusPublishingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            // 默认每5分钟发布一次健康状态
            _publishInterval = TimeSpan.FromMinutes(5);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Health Status Publishing Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PublishHealthStatus(stoppingToken);
                    
                    // 等待指定间隔
                    await Task.Delay(_publishInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // 预期的停止信号
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while publishing health status.");
                    
                    // 出错后等待较长时间再重试，避免频繁错误
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // 预期的停止信号
                        break;
                    }
                }
            }

            _logger.LogInformation("Health Status Publishing Service is stopping.");
        }

        private async Task PublishHealthStatus(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var healthCheckService = scope.ServiceProvider.GetRequiredService<IHealthCheckService>();

            try
            {
                var healthStatus = await healthCheckService.GetHealthStatusAsync();
                
                // 记录健康状态到日志
                _logger.LogInformation("Health Status Published - Overall: {Status}, CheckedAt: {CheckedAt}, Components: {@Components}",
                    healthStatus.Status, healthStatus.CheckedAt, healthStatus.Components);

                // 这里可以添加更多发布逻辑，如：
                // 1. 将状态发布到外部监控系统
                // 2. 写入特定的健康状态文件
                // 3. 发送到消息队列
                // 4. 调用外部Webhook等
                
                await PublishToExternalSystems(healthStatus, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish health status.");
                throw;
            }
        }

        private async Task PublishToExternalSystems(HealthStatus healthStatus, CancellationToken cancellationToken)
        {
            // 示例：将健康状态写入本地文件（用于外部监控工具读取）
            try
            {
                var statusFilePath = Path.Combine(Directory.GetCurrentDirectory(), "health-status.json");
                var statusData = new
                {
                    Status = healthStatus.Status,
                    CheckedAt = healthStatus.CheckedAt,
                    ServiceName = healthStatus.ServiceName,
                    Version = healthStatus.Version,
                    Components = healthStatus.Components,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(statusData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(statusFilePath, json, cancellationToken);
                
                _logger.LogDebug("Health status written to {FilePath}", statusFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write health status to file.");
            }

            // 示例：如果有外部监控系统，可以在这里添加HTTP调用
            // 示例代码如下（注释掉以避免实际调用）：
            /*
            try
            {
                using var httpClient = new HttpClient();
                var payload = JsonSerializer.Serialize(healthStatus, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://your-monitoring-system.com/api/health", content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to publish health status to external system. Status: {StatusCode}", response.StatusCode);
                }
                else
                {
                    _logger.LogDebug("Successfully published health status to external system.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not publish health status to external system.");
            }
            */
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Health Status Publishing Service is stopping.");
            await base.StopAsync(cancellationToken);
        }
    }
}