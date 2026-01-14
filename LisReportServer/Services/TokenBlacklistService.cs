using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace LisReportServer.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<TokenBlacklistService> _logger;
        
        // 内存中的黑名单存储
        private static readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = 
            new ConcurrentDictionary<string, DateTime>();

        public TokenBlacklistService(IMemoryCache memoryCache, ILogger<TokenBlacklistService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string tokenId)
        {
            // 检查内存中的黑名单
            if (_blacklistedTokens.TryGetValue(tokenId, out DateTime expiry))
            {
                if (DateTime.UtcNow <= expiry)
                {
                    return true; // 仍在黑名单中
                }
                else
                {
                    // 已过期，从黑名单中移除
                    _blacklistedTokens.TryRemove(tokenId, out _);
                    return false;
                }
            }

            // 检查内存缓存中的黑名单（作为备份）
            if (_memoryCache.TryGetValue($"blacklist_{tokenId}", out bool isBlacklisted))
            {
                return isBlacklisted;
            }

            return false;
        }

        public async Task AddTokenToBlacklistAsync(string tokenId, DateTime expirationTime)
        {
            // 添加到内存中的黑名单
            _blacklistedTokens[tokenId] = expirationTime;

            // 同时添加到内存缓存中（作为备份）
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expirationTime
            };
            _memoryCache.Set($"blacklist_{tokenId}", true, cacheOptions);

            _logger.LogInformation($"Token {tokenId} added to blacklist until {expirationTime}");
        }

        public async Task RemoveExpiredTokensAsync()
        {
            var now = DateTime.UtcNow;
            var expiredTokens = _blacklistedTokens
                .Where(kvp => kvp.Value <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var tokenId in expiredTokens)
            {
                _blacklistedTokens.TryRemove(tokenId, out _);
            }

            _logger.LogInformation($"Removed {expiredTokens.Count} expired tokens from blacklist");
        }

        // 辅助方法：使特定用户的令牌失效
        public async Task InvalidateUserTokensAsync(string userId)
        {
            // 为用户生成一个唯一的失效标记
            var invalidationKey = $"invalidation_{userId}_{DateTime.UtcNow.Ticks}";
            var expirationTime = DateTime.UtcNow.AddHours(24); // 24小时后过期
            
            await AddTokenToBlacklistAsync(invalidationKey, expirationTime);
            
            _logger.LogInformation($"Invalidated tokens for user {userId}");
        }
    }
}