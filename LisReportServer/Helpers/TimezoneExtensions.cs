using LisReportServer.Services;
using Microsoft.AspNetCore.Http;

namespace LisReportServer.Helpers
{
    public static class TimezoneExtensions
    {
        /// <summary>
        /// 扩展方法，用于将UTC时间转换为客户端时区时间
        /// </summary>
        /// <param name="httpContext">HTTP上下文</param>
        /// <param name="utcDateTime">UTC时间</param>
        /// <returns>客户端时区时间</returns>
        public static DateTime ToClientTime(this HttpContext httpContext, DateTime utcDateTime)
        {
            if (httpContext.RequestServices.GetService(typeof(ITimezoneService)) is ITimezoneService timezoneService)
            {
                return timezoneService.ConvertUtcToClientTime(utcDateTime);
            }

            // 如果服务不可用，返回本地时间
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);
        }

        /// <summary>
        /// 扩展方法，用于获取当前请求的时区偏移
        /// </summary>
        /// <param name="httpContext">HTTP上下文</param>
        /// <returns>时区偏移字符串</returns>
        public static string GetTimezoneOffset(this HttpContext httpContext)
        {
            if (httpContext.RequestServices.GetService(typeof(ITimezoneService)) is ITimezoneService timezoneService)
            {
                return timezoneService.GetCurrentTimezoneOffset();
            }

            // 如果服务不可用，返回本地时区偏移
            var offset = TimeZoneInfo.Local.BaseUtcOffset;
            var sign = offset >= TimeSpan.Zero ? "+" : "-";
            return $"{sign}{offset:hh\\:mm}";
        }
    }
}