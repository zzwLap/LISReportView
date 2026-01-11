using Microsoft.AspNetCore.Mvc;
using LisReportServer.Services;

namespace LisReportServer.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportApiController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportApiController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("patients")]
        public async Task<IActionResult> GetPatientsAsync([FromQuery] string? patientId = null, [FromQuery] string? examId = null, [FromQuery] string? outpatientId = null)
        {
            var allPatients = await _reportService.GetTodayPatientsAsync();
            var totalPatientCount = allPatients.Count;

            var filteredPatients = allPatients.AsQueryable();

            if (!string.IsNullOrEmpty(patientId))
            {
                filteredPatients = filteredPatients.Where(p => p.PatientId.Contains(patientId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(examId))
            {
                filteredPatients = filteredPatients.Where(p => p.ExamId.Contains(examId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(outpatientId))
            {
                filteredPatients = filteredPatients.Where(p => p.OutpatientId.Contains(outpatientId, StringComparison.OrdinalIgnoreCase));
            }

            var hasQueryParams = !string.IsNullOrEmpty(patientId) || !string.IsNullOrEmpty(examId) || !string.IsNullOrEmpty(outpatientId);
            var result = new
            {
                Patients = filteredPatients.ToList(),
                TotalCount = totalPatientCount,
                FilteredCount = filteredPatients.Count(),
                HasQueryParams = hasQueryParams,
                IsFiltered = hasQueryParams && filteredPatients.Any() // 表示有查询条件且有结果
            };

            return Ok(result);
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReportsAsync([FromQuery] string? patientId = null, [FromQuery] string? examId = null, [FromQuery] string? outpatientId = null)
        {
            var allReports = await _reportService.GetAllTodayReportsAsync();
            var totalReportCount = allReports.Count;

            var filteredReports = allReports.AsQueryable();

            if (!string.IsNullOrEmpty(patientId))
            {
                filteredReports = filteredReports.Where(r => r.PatientId.Contains(patientId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(examId))
            {
                filteredReports = filteredReports.Where(r => r.ExamId.Contains(examId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(outpatientId))
            {
                filteredReports = filteredReports.Where(r => r.OutpatientId.Contains(outpatientId, StringComparison.OrdinalIgnoreCase));
            }

            var hasQueryParams = !string.IsNullOrEmpty(patientId) || !string.IsNullOrEmpty(examId) || !string.IsNullOrEmpty(outpatientId);
            var result = new
            {
                Reports = filteredReports.ToList(),
                TotalCount = totalReportCount,
                FilteredCount = filteredReports.Count(),
                HasQueryParams = hasQueryParams,
                IsFiltered = hasQueryParams && filteredReports.Any() // 表示有查询条件且有结果
            };

            return Ok(result);
        }
    }
}