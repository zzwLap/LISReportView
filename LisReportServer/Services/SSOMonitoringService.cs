using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace LisReportServer.Services
{
    public class SSOMonitoringService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SSOMonitoringService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        private const string SSO_STATUS_CACHE_KEY = "SSO_STATUS_MONITOR";

        public SSOMonitoringService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<SSOMonitoringService> logger,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(
                _configuration.GetValue<int>("SSOSettings:HealthCheckIntervalSeconds", 30));

            _logger.LogInformation("SSO监控服务启动，检查间隔: {Interval}秒", interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var healthCheckService = scope.ServiceProvider.GetRequiredService<ISSOHealthCheckService>();
                    
                    var isAvailable = await healthCheckService.IsSSOAvailableAsync();
                    
                    // 更新缓存状态
                    _cache.Set(SSO_STATUS_CACHE_KEY, isAvailable, TimeSpan.FromMinutes(5));
                    
                    _logger.LogDebug("SSO认证中心状态: {Status}", isAvailable ? "可用" : "不可用");

                    await Task.Delay(interval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "监控SSO状态时发生错误");
                    // 发生错误时等待相同的时间间隔再重试
                    await Task.Delay(interval, stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SSO监控服务停止");
            await base.StopAsync(cancellationToken);
        }
    }
}