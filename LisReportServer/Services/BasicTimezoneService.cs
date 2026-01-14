using Microsoft.AspNetCore.Http;
using LisReportServer.Helpers;

namespace LisReportServer.Services
{
    /// <summary>
    /// 基础时区服务 - 仅提供基本的时区转换功能
    /// </summary>
    public class BasicTimezoneService : ITimezoneService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BasicTimezoneService> _logger;

        public BasicTimezoneService(IHttpContextAccessor httpContextAccessor, ILogger<BasicTimezoneService> logger)
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
                var timezoneInfo = TimezoneHelper.GetTimeZoneInfoFromIana(timezoneId);
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
            var offset = timezoneInfo.BaseUtcOffset;
            var sign = offset >= TimeSpan.Zero ? "+" : "-";
            return $"{sign}{offset:hh\\:mm}";
        }
    }
}