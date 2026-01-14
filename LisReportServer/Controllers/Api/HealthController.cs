using Microsoft.AspNetCore.Mvc;
using LisReportServer.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using LisReportServer.Helpers;

namespace LisReportServer.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IHealthCheckService healthCheckService, ITimezoneService timezoneService, ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _timezoneService = timezoneService;
            _logger = logger;
        }

        /// <summary>
        /// 获取系统健康状态
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(HealthStatus), 200)]
        public async Task<ActionResult<HealthStatus>> GetHealth([FromQuery]string timezone = null)
        {
            try
            {
                var healthStatus = await _healthCheckService.GetHealthStatusAsync();
                
                // 使用时区服务来处理时区转换
                if (!string.IsNullOrEmpty(timezone))
                {
                    var clientTimezoneInfo = TimezoneHelper.GetTimeZoneInfoFromIana(timezone);
                    if (clientTimezoneInfo != null)
                    {
                        healthStatus.SetClientTimezoneInfo(clientTimezoneInfo);
                    }
                }
                else
                {
                    // 使用当前请求的时区设置
                    var currentTimezoneInfo = _timezoneService.GetCurrentTimezone();
                    healthStatus.SetClientTimezoneInfo(currentTimezoneInfo);
                }
                
                var statusCode = healthStatus.Status switch
                {
                    "Healthy" => 200,
                    "Degraded" => 200, // 或者 503，取决于业务需求
                    "Unhealthy" => 503,
                    _ => 200
                };

                Response.StatusCode = statusCode;
                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status");
                return StatusCode(500, new HealthStatus 
                { 
                    Status = "Unhealthy", 
                    CheckedAtUtc = DateTime.UtcNow,
                    Components = new Dictionary<string, object>
                    {
                        { "error", new { status = "Unhealthy", message = ex.Message } }
                    }
                });
            }
        }

        /// <summary>
        /// 获取详细健康状态
        /// </summary>
        [HttpGet("details")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        public async Task<ActionResult<Dictionary<string, object>>> GetHealthDetails()
        {
            try
            {
                var detailedStatus = _healthCheckService.GetDetailedHealthStatus();
                var healthStatus = detailedStatus.ContainsKey("status") ? detailedStatus["status"].ToString() : "Unknown";
                
                var statusCode = healthStatus switch
                {
                    "Healthy" => 200,
                    "Degraded" => 200,
                    "Unhealthy" => 503,
                    _ => 200
                };

                Response.StatusCode = statusCode;
                return Ok(detailedStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health details");
                return StatusCode(500, new Dictionary<string, object>
                {
                    { "status", "Unhealthy" },
                    { "error", ex.Message }
                });
            }
        }

        /// <summary>
        /// 快速健康检查 - 仅返回HTTP状态码
        /// </summary>
        [HttpGet("ping")]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public IActionResult Ping()
        {
            // 简单返回200 OK，表示服务正在运行
            return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
        }
    }
}