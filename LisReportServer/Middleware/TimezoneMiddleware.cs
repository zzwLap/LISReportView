using Microsoft.AspNetCore.Http;
using System.Globalization;
using LisReportServer.Helpers;

namespace LisReportServer.Middleware
{
    public class TimezoneMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TimezoneMiddleware> _logger;

        public TimezoneMiddleware(RequestDelegate next, ILogger<TimezoneMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 尝试从请求头或查询参数中获取时区信息
            var timezone = GetTimezoneFromRequest(context);

            if (!string.IsNullOrEmpty(timezone))
            {
                // 将时区信息存储到HttpContext.Items中，供后续处理程序使用
                context.Items["ClientTimezone"] = timezone;
                
                try
                {
                    var timeZoneInfo = TimezoneHelper.GetTimeZoneInfoFromIana(timezone);
                    if (timeZoneInfo != null)
                    {
                        context.Items["ClientTimezoneInfo"] = timeZoneInfo;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid timezone provided: {Timezone}", timezone);
                }
            }

            await _next(context);
        }

        private string GetTimezoneFromRequest(HttpContext context)
        {
            // 优先级：1. 查询参数 2. 请求头 3. Cookie
            var timezone = context.Request.Query["timezone"].FirstOrDefault() ??
                          context.Request.Headers["X-Timezone"].FirstOrDefault() ??
                          context.Request.Cookies["timezone"];

            return timezone ?? "";
        }
    }
}