using Microsoft.Extensions.Caching.Memory;

namespace LisReportServer.Services
{
    public class CachedSSOHealthCheckService : ISSOHealthCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedSSOHealthCheckService> _logger;
        
        private const string CACHE_KEY = "SSO_HEALTH_STATUS";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(30);

        public CachedSSOHealthCheckService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            IMemoryCache cache,
            ILogger<CachedSSOHealthCheckService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsSSOAvailableAsync()
        {
            // 先從緩存獲取
            if (_cache.TryGetValue(CACHE_KEY, out bool cachedResult))
            {
                return cachedResult;
            }

            // 緩存未命中，執行實際檢查
            var result = await CheckSSOHealthAsync();
            
            // 將結果存入緩存
            _cache.Set(CACHE_KEY, result, CACHE_DURATION);
            
            return result;
        }

        private async Task<bool> CheckSSOHealthAsync()
        {
            try
            {
                var authority = _configuration["SSOSettings:Authority"];
                if (string.IsNullOrEmpty(authority))
                {
                    return false;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10秒超時
                var metadataEndpoint = $"{authority.TrimEnd('/')}/.well-known/openid_configuration";
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