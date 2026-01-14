using Microsoft.AspNetCore.Mvc;
using LisReportServer.Services;

namespace LisReportServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemoTimezoneController : ControllerBase
    {
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<DemoTimezoneController> _logger;

        public DemoTimezoneController(ITimezoneService timezoneService, ILogger<DemoTimezoneController> logger)
        {
            _timezoneService = timezoneService;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前请求的时区信息
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetTimezoneInfo()
        {
            try
            {
                var timezoneInfo = _timezoneService.GetCurrentTimezone();
                var timezoneOffset = _timezoneService.GetCurrentTimezoneOffset();
                
                var result = new
                {
                    ServerTime = DateTime.Now,
                    UtcTime = DateTime.UtcNow,
                    ClientTime = _timezoneService.ConvertUtcToClientTime(DateTime.UtcNow),
                    TimezoneId = timezoneInfo.Id,
                    TimezoneName = timezoneInfo.DisplayName,
                    TimezoneOffset = timezoneOffset,
                    IsDaylightSavingTime = timezoneInfo.IsDaylightSavingTime(DateTime.Now)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timezone info");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// 演示时间转换功能
        /// </summary>
        [HttpGet("convert")]
        public IActionResult ConvertTime([FromQuery] string timezone = null)
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var result = new
                {
                    UtcTime = utcNow,
                    ConvertedTime = !string.IsNullOrEmpty(timezone) 
                        ? _timezoneService.ConvertUtcToTimezone(utcNow, timezone) 
                        : _timezoneService.ConvertUtcToClientTime(utcNow),
                    TimezoneOffset = !string.IsNullOrEmpty(timezone) 
                        ? GetTimezoneOffset(timezone) 
                        : _timezoneService.GetCurrentTimezoneOffset(),
                    OriginalTimezone = timezone ?? "Auto-detected"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting time");
                return StatusCode(500, new { error = "Time conversion error" });
            }
        }

        private string GetTimezoneOffset(string timezoneId)
        {
            try
            {
                var timezoneInfo = LisReportServer.Helpers.TimezoneHelper.GetTimeZoneInfoFromIana(timezoneId);
                if (timezoneInfo != null)
                {
                    var offset = timezoneInfo.BaseUtcOffset;
                    var sign = offset >= TimeSpan.Zero ? "+" : "-";
                    return $"{sign}{offset:hh\\:mm}";
                }
            }
            catch
            {
                // 如果转换失败，返回UTC
            }
            return "+00:00";
        }
    }
}