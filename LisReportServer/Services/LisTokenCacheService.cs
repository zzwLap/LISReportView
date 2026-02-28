using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace LisReportServer.Services
{
    /// <summary>
    /// LIS登录Token缓存服务实现
    /// </summary>
    public class LisTokenCacheService : ILisTokenCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<LisTokenCacheService> _logger;

        // 用于跟踪所有Token的键，方便清理过期Token
        private static readonly ConcurrentDictionary<string, DateTime> _tokenKeys = new ConcurrentDictionary<string, DateTime>();

        public LisTokenCacheService(IMemoryCache memoryCache, ILogger<LisTokenCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public Task SetTokenAsync(string hospitalName, string username, string accessToken, string? refreshToken, int expirationMinutes = 30)
        {
            var cacheKey = BuildCacheKey(hospitalName, username);
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var tokenInfo = new LisTokenInfo
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt
            };
            
            // 注册缓存移除回调
            cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (key is string k)
                {
                    _tokenKeys.TryRemove(k, out _);
                    _logger.LogDebug("LIS Token缓存已移除: {CacheKey}, 原因: {Reason}", k, reason);
                }
            });

            _memoryCache.Set(cacheKey, tokenInfo, cacheOptions);
            _tokenKeys[cacheKey] = expiresAt;

            _logger.LogInformation("LIS Token已缓存: 医院={HospitalName}, 用户={Username}, 过期时间={ExpiresAt}",
                hospitalName, username, expiresAt);

            return Task.CompletedTask;
        }

        public Task<LisTokenInfo?> GetTokenAsync(string hospitalName, string username)
        {
            var cacheKey = BuildCacheKey(hospitalName, username);

            if (_memoryCache.TryGetValue(cacheKey, out LisTokenInfo? tokenInfo))
            {
                // 检查是否过期
                if (tokenInfo != null && tokenInfo.ExpiresAt > DateTime.UtcNow)
                {
                    _logger.LogDebug("从缓存获取LIS Token: 医院={HospitalName}, 用户={Username}", hospitalName, username);
                    return Task.FromResult<LisTokenInfo?>(tokenInfo);
                }
                else
                {
                    // 已过期，从缓存中移除
                    _memoryCache.Remove(cacheKey);
                    _tokenKeys.TryRemove(cacheKey, out _);
                    _logger.LogDebug("LIS Token已过期: 医院={HospitalName}, 用户={Username}", hospitalName, username);
                }
            }

            return Task.FromResult<LisTokenInfo?>(null);
        }

        public Task RemoveTokenAsync(string hospitalName, string username)
        {
            var cacheKey = BuildCacheKey(hospitalName, username);
            _memoryCache.Remove(cacheKey);
            _tokenKeys.TryRemove(cacheKey, out _);

            _logger.LogInformation("LIS Token已清除: 医院={HospitalName}, 用户={Username}", hospitalName, username);

            return Task.CompletedTask;
        }

        public Task RemoveExpiredTokensAsync()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _tokenKeys
                .Where(kvp => kvp.Value <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _memoryCache.Remove(key);
                _tokenKeys.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogInformation("已清理 {Count} 个过期的LIS Token", expiredKeys.Count);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 构建缓存键
        /// </summary>
        private string BuildCacheKey(string hospitalName, string username)
        {
            return $"lis_token_{hospitalName}_{username}";
        }
    }
}
