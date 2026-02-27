using System.Net.Http;

namespace LisReportServer.Services
{
    public interface ISSOHealthCheckService
    {
        Task<bool> IsSSOAvailableAsync();
    }

    public class SSOHealthCheckService : ISSOHealthCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SSOHealthCheckService> _logger;

        public SSOHealthCheckService(HttpClient httpClient, IConfiguration configuration, ILogger<SSOHealthCheckService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> IsSSOAvailableAsync()
        {
            try
            {
                var authority = _configuration["SSOSettings:Authority"];
                if (string.IsNullOrEmpty(authority))
                {
                    return false;
                }

                // 尝試訪問SSO認證中心的元數據端點
                var metadataEndpoint = $"{authority.TrimEnd('/')}/.well-known/openid_configuration";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10秒超時
                var response = await _httpClient.GetAsync(metadataEndpoint, cts.Token);
                
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("SSO認證中心檢查超時");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SSO認證中心不可用");
                return false;
            }
        }
    }
}