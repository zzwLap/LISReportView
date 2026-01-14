using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LisReportServer.Services
{
    public class TokenBlacklistCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenBlacklistCleanupService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(1); // 每小时清理一次

        public TokenBlacklistCleanupService(IServiceProvider serviceProvider, ILogger<TokenBlacklistCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_period);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var tokenBlacklistService = scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>();
                    
                    await tokenBlacklistService.RemoveExpiredTokensAsync();
                    _logger.LogInformation("Token blacklist cleanup completed at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up token blacklist");
                }

                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
    }
}