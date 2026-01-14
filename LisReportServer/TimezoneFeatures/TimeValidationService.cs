using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Services
{
    /// <summary>
    /// 时间验证和标准化服务
    /// </summary>
    public interface ITimeValidationService
    {
        /// <summary>
        /// 验证时间格式的有效性
        /// </summary>
        bool IsValidTimeFormat(string timeString);
        
        /// <summary>
        /// 将时间字符串标准化为DateTime对象
        /// </summary>
        DateTime? ParseAndStandardizeTime(string timeString, string format = null);
        
        /// <summary>
        /// 验证时间范围
        /// </summary>
        ValidationResult ValidateTimeRange(DateTime startTime, DateTime endTime, DateTime? maxRange = null);
        
        /// <summary>
        /// 标准化时间精度（例如舍入到分钟级别）
        /// </summary>
        DateTime StandardizePrecision(DateTime dateTime, TimeSpan precision = default);
    }

    public class TimeValidationService : ITimeValidationService
    {
        private readonly ILogger<TimeValidationService> _logger;

        public TimeValidationService(ILogger<TimeValidationService> logger)
        {
            _logger = logger;
        }

        public bool IsValidTimeFormat(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return false;

            // 尝试解析多种常见的时间格式
            var formats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",      // 2023-12-25 14:30:00
                "yyyy-MM-ddTHH:mm:ss",      // 2023-12-25T14:30:00
                "yyyy-MM-ddTHH:mm:ssZ",     // ISO 8601 UTC: 2023-12-25T14:30:00Z
                "yyyy-MM-ddTHH:mm:ss.fffZ", // ISO 8601 with milliseconds: 2023-12-25T14:30:00.123Z
                "MM/dd/yyyy HH:mm:ss",      // US format: 12/25/2023 14:30:00
                "dd/MM/yyyy HH:mm:ss",      // EU format: 25/12/2023 14:30:00
                "yyyy-MM-dd",               // Date only: 2023-12-25
                "HH:mm:ss",                 // Time only: 14:30:00
                "yyyy-MM-ddTHH:mm:ss.fffzzz", // ISO 8601 with timezone: 2023-12-25T14:30:00.123+08:00
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(timeString, format, null, DateTimeStyles.None, out _))
                {
                    return true;
                }
            }

            // 尝试使用通用解析
            return DateTime.TryParse(timeString, out _);
        }

        public DateTime? ParseAndStandardizeTime(string timeString, string format = null)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            try
            {
                if (!string.IsNullOrEmpty(format))
                {
                    if (DateTime.TryParseExact(timeString, format, null, DateTimeStyles.None, out var parsedTime))
                    {
                        return parsedTime;
                    }
                }
                else
                {
                    // 尝试解析多种格式
                    var formats = new[]
                    {
                        "yyyy-MM-dd HH:mm:ss",
                        "yyyy-MM-ddTHH:mm:ss",
                        "yyyy-MM-ddTHH:mm:ssZ",
                        "yyyy-MM-ddTHH:mm:ss.fffZ",
                        "MM/dd/yyyy HH:mm:ss",
                        "dd/MM/yyyy HH:mm:ss",
                        "yyyy-MM-dd",
                        "yyyy-MM-ddTHH:mm:ss.fffzzz",
                    };

                    foreach (var fmt in formats)
                    {
                        if (DateTime.TryParseExact(timeString, fmt, null, DateTimeStyles.None, out var parsedTime))
                        {
                            return parsedTime;
                        }
                    }

                    // 最后尝试通用解析
                    if (DateTime.TryParse(timeString, out var generalParsedTime))
                    {
                        return generalParsedTime;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse time string: {TimeString}", timeString);
            }

            return null;
        }

        public ValidationResult ValidateTimeRange(DateTime startTime, DateTime endTime, DateTime? maxRange = null)
        {
            if (startTime > endTime)
            {
                return new ValidationResult("开始时间不能晚于结束时间");
            }

            if (maxRange.HasValue)
            {
                var range = endTime - startTime;
                if (range > maxRange.Value.Subtract(DateTime.MinValue))
                {
                    return new ValidationResult($"时间范围不能超过 {maxRange.Value.Subtract(DateTime.MinValue)}");
                }
            }

            return ValidationResult.Success;
        }

        public DateTime StandardizePrecision(DateTime dateTime, TimeSpan precision = default)
        {
            // 默认精度为分钟级别
            if (precision == default(TimeSpan))
            {
                precision = TimeSpan.FromMinutes(1);
            }

            var ticks = (long)(dateTime.Ticks / precision.Ticks) * precision.Ticks;
            return new DateTime(ticks);
        }
    }
}