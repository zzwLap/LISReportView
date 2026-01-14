using System.Globalization;

namespace LisReportServer.Helpers
{
    public static class TimezoneHelper
    {
        /// <summary>
        /// 根据客户端提供的时区偏移量将UTC时间转换为本地时间
        /// </summary>
        /// <param name="utcDateTime">UTC时间</param>
        /// <param name="timezoneOffsetMinutes">时区偏移量（分钟），例如东八区为480</param>
        /// <returns>转换后的本地时间</returns>
        public static DateTime ConvertUtcToLocalTime(DateTime utcDateTime, int timezoneOffsetMinutes = 0)
        {
            var offset = TimeSpan.FromMinutes(timezoneOffsetMinutes);
            var utcAsOffset = DateTimeOffset.UtcNow.ToOffset(offset);
            
            // 更准确的实现：直接使用时区偏移
            return utcDateTime.AddMinutes(timezoneOffsetMinutes);
        }
        
        /// <summary>
        /// 从IANA时区名称获取TimeZoneInfo
        /// </summary>
        /// <param name="ianaTimeZoneId">IANA时区ID，如 "Asia/Shanghai"</param>
        /// <returns>TimeZoneInfo对象</returns>
        public static TimeZoneInfo? GetTimeZoneInfoFromIana(string ianaTimeZoneId)
        {
            try
            {
                // 尝试直接获取Windows时区
                var windowsTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
                return windowsTimeZone;
            }
            catch
            {
                // 如果直接匹配失败，尝试映射IANA到Windows时区
                var mapping = GetIanaToWindowsMapping();
                if (mapping.ContainsKey(ianaTimeZoneId))
                {
                    try
                    {
                        return TimeZoneInfo.FindSystemTimeZoneById(mapping[ianaTimeZoneId]);
                    }
                    catch
                    {
                        // 如果映射失败，返回本地时区
                        return TimeZoneInfo.Local;
                    }
                }
                return TimeZoneInfo.Local; // 默认返回本地时区
            }
        }
        
        /// <summary>
        /// 获取IANA到Windows时区的映射
        /// </summary>
        private static Dictionary<string, string> GetIanaToWindowsMapping()
        {
            return new Dictionary<string, string>
            {
                { "Asia/Shanghai", "China Standard Time" },
                { "Asia/Tokyo", "Tokyo Standard Time" },
                { "America/New_York", "Eastern Standard Time" },
                { "America/Los_Angeles", "Pacific Standard Time" },
                { "Europe/London", "GMT Standard Time" },
                { "Europe/Paris", "Romance Standard Time" },
                { "Asia/Kolkata", "India Standard Time" }
            };
        }
    }
}