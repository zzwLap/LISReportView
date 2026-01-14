using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using LisReportServer.Helpers;

namespace LisReportServer.Services
{
    /// <summary>
    /// 性能优化的时区服务，使用缓存减少重复的时区转换计算
    /// </summary>
    public class OptimizedTimezoneService : ITimezoneService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OptimizedTimezoneService> _logger;
        
        // 缓存时区信息以提高性能
        private static readonly ConcurrentDictionary<string, TimeZoneInfo> _timezoneCache = 
            new ConcurrentDictionary<string, TimeZoneInfo>();
        
        // 缓存时区偏移字符串以提高性能
        private static readonly ConcurrentDictionary<string, string> _timezoneOffsetCache = 
            new ConcurrentDictionary<string, string>();

        public OptimizedTimezoneService(IHttpContextAccessor httpContextAccessor, ILogger<OptimizedTimezoneService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public DateTime ConvertUtcToClientTime(DateTime utcDateTime)
        {
            try
            {
                var timezoneInfo = GetCurrentTimezone();
                if (timezoneInfo != null)
                {
                    // 使用缓存的时区转换，避免重复计算
                    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timezoneInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert UTC time to client timezone");
            }

            // 如果转换失败，返回本地时间
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);
        }

        public DateTime ConvertUtcToTimezone(DateTime utcDateTime, string timezoneId)
        {
            try
            {
                var timezoneInfo = GetTimezoneInfoFromCache(timezoneId);
                if (timezoneInfo != null)
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timezoneInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert UTC time to timezone: {TimezoneId}", timezoneId);
            }

            // 如果转换失败，返回本地时间
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);
        }

        public TimeZoneInfo GetCurrentTimezone()
        {
            if (_httpContextAccessor.HttpContext?.Items["ClientTimezoneInfo"] is TimeZoneInfo timezoneInfo)
            {
                return timezoneInfo;
            }

            // 如果没有客户端时区信息，返回服务器本地时区
            return TimeZoneInfo.Local;
        }

        public string GetCurrentTimezoneOffset()
        {
            var timezoneInfo = GetCurrentTimezone();
            var timezoneId = timezoneInfo.Id;
            
            // 使用缓存的时区偏移，避免重复计算
            return _timezoneOffsetCache.GetOrAdd(timezoneId, id =>
            {
                var offset = timezoneInfo.BaseUtcOffset;
                var sign = offset >= TimeSpan.Zero ? "+" : "-";
                return $"{sign}{offset:hh\\:mm}";
            });
        }

        private TimeZoneInfo GetTimezoneInfoFromCache(string timezoneId)
        {
            if (string.IsNullOrEmpty(timezoneId))
            {
                return null;
            }

            // 使用缓存避免重复解析时区信息
            return _timezoneCache.GetOrAdd(timezoneId, id =>
            {
                try
                {
                    return TimezoneHelper.GetTimeZoneInfoFromIana(id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get timezone info for ID: {TimezoneId}", id);
                    return null;
                }
            });
        }
    }
}