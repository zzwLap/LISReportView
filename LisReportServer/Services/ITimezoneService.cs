using System.Globalization;

namespace LisReportServer.Services
{
    public interface ITimezoneService
    {
        /// <summary>
        /// 将UTC时间转换为客户端时区时间
        /// </summary>
        /// <param name="utcDateTime">UTC时间</param>
        /// <returns>客户端时区时间</returns>
        DateTime ConvertUtcToClientTime(DateTime utcDateTime);

        /// <summary>
        /// 将UTC时间转换为指定时区时间
        /// </summary>
        /// <param name="utcDateTime">UTC时间</param>
        /// <param name="timezoneId">时区ID</param>
        /// <returns>指定时区时间</returns>
        DateTime ConvertUtcToTimezone(DateTime utcDateTime, string timezoneId);

        /// <summary>
        /// 获取当前请求的时区信息
        /// </summary>
        /// <returns>时区信息</returns>
        TimeZoneInfo GetCurrentTimezone();

        /// <summary>
        /// 获取当前时区偏移字符串
        /// </summary>
        /// <returns>时区偏移字符串</returns>
        string GetCurrentTimezoneOffset();
    }
}