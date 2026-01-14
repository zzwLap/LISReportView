using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace LisReportServer.Services
{
    public class AdvancedTokenBlacklistService : ITokenBlacklistService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AdvancedTokenBlacklistService> _logger;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly IDatabase _redisDatabase;
        private readonly bool _useRedis;

        // 内存中的黑名单存储（备用）
        private static readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = 
            new ConcurrentDictionary<string, DateTime>();

        public AdvancedTokenBlacklistService(IMemoryCache memoryCache, ILogger<AdvancedTokenBlacklistService> logger, IConnectionMultiplexer redisConnection)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _redisConnection = redisConnection;

            // 检查Redis连接是否可用
            try
            {
                _redisDatabase = _redisConnection.GetDatabase();
                // 尝试ping Redis服务器
                var pingResult = _redisConnection.GetServer(_redisConnection.GetEndPoints()[0]).Ping();
                _useRedis = true;
                _logger.LogInformation("Redis connection established successfully. Using Redis for token blacklisting.");
            }
            catch (Exception ex)
            {
                _useRedis = false;
                _logger.LogWarning("Could not connect to Redis ({Message}). Falling back to in-memory storage.", ex.Message);
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string tokenId)
        {
            if (_useRedis)
            {
                try
                {
                    var isBlacklisted = await _redisDatabase.StringGetAsync($"blacklist:{tokenId}");
                    if (isBlacklisted.IsNullOrEmpty)
                    {
                        // 检查内存中的黑名单（备用）
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
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Redis error ({Message}), falling back to in-memory storage for checking token {TokenId}.", ex.Message, tokenId);
                    // 如果Redis出错，回退到内存检查
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
                    return false;
                }
            }
            else
            {
                // 使用内存存储
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
        }

        public async Task AddTokenToBlacklistAsync(string tokenId, DateTime expirationTime)
        {
            var ttl = expirationTime - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
            {
                return; // 已过期，无需添加
            }

            if (_useRedis)
            {
                try
                {
                    // 添加到Redis
                    await _redisDatabase.StringSetAsync(
                        $"blacklist:{tokenId}", 
                        "1", 
                        expiry: ttl);
                    
                    _logger.LogInformation($"Token {tokenId} added to Redis blacklist with TTL {ttl}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Redis error ({Message}), adding token {TokenId} to in-memory blacklist instead.", ex.Message, tokenId);
                    // 如果Redis出错，添加到内存存储
                    _blacklistedTokens[tokenId] = expirationTime;
                    
                    // 同时添加到内存缓存中（作为备份）
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = expirationTime
                    };
                    _memoryCache.Set($"blacklist_{tokenId}", true, cacheOptions);
                }
            }
            else
            {
                // 添加到内存中的黑名单
                _blacklistedTokens[tokenId] = expirationTime;

                // 同时添加到内存缓存中（作为备份）
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expirationTime
                };
                _memoryCache.Set($"blacklist_{tokenId}", true, cacheOptions);

                _logger.LogInformation($"Token {tokenId} added to in-memory blacklist until {expirationTime}");
            }
        }

        public async Task RemoveExpiredTokensAsync()
        {
            if (!_useRedis)
            {
                // 只对内存存储进行清理
                var now = DateTime.UtcNow;
                var expiredTokens = _blacklistedTokens
                    .Where(kvp => kvp.Value <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var tokenId in expiredTokens)
                {
                    _blacklistedTokens.TryRemove(tokenId, out _);
                }

                _logger.LogInformation($"Removed {expiredTokens.Count} expired tokens from in-memory blacklist");
            }
            // Redis中的键会自动过期，无需手动清理
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