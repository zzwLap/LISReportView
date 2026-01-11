using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LisReportServer.Services;

namespace LisReportServer.Pages
{
    public class ReportsDashboardModel : PageModel
    {
        private readonly IReportService _reportService;

        public ReportsDashboardModel(IReportService reportService)
        {
            _reportService = reportService;
            Summary = new ReportSummary();
            Reports = new List<ReportRecord>();
        }

        [BindProperty(SupportsGet = true)]
        public string? PatientId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ExamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? OutpatientId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PatientIdForPatientList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ExamIdForPatientList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? OutpatientIdForPatientList { get; set; }

        public ReportSummary Summary { get; set; }
        public List<ReportRecord> Reports { get; set; }
        public bool HasQueryParamsForReports { get; set; } = false;
        public int TotalReportCount { get; set; } = 0;
        public List<PatientInfo> TodayPatients { get; set; } = new();
        public bool HasQueryParamsForPatients { get; set; } = false;
        public int TotalPatientCount { get; set; } = 0;

        public async Task<ActionResult> OnGetAsync()
        {
            // 获取今日统计信息
            Summary = await _reportService.GetTodaySummaryAsync();

            // 获取所有今日报告信息
            var allReports = await _reportService.GetAllTodayReportsAsync();
            TotalReportCount = allReports.Count;
            
            // 判断是否有查询参数
            HasQueryParamsForReports = !string.IsNullOrEmpty(PatientId) || 
                                   !string.IsNullOrEmpty(ExamId) || 
                                   !string.IsNullOrEmpty(OutpatientId);
            
            // 根据查询条件获取报告列表
            if (!string.IsNullOrEmpty(PatientId))
            {
                Reports = await _reportService.GetReportsByPatientIdAsync(PatientId);
            }
            else if (!string.IsNullOrEmpty(ExamId))
            {
                Reports = await _reportService.GetReportsByExamIdAsync(ExamId);
            }
            else if (!string.IsNullOrEmpty(OutpatientId))
            {
                Reports = await _reportService.GetReportsByOutpatientIdAsync(OutpatientId);
            }
            else
            {
                Reports = allReports;
            }
            
            // 获取所有今日患者信息
            var allPatients = await _reportService.GetTodayPatientsAsync();
            TotalPatientCount = allPatients.Count;
            
            // 判断是否有查询参数
            HasQueryParamsForPatients = !string.IsNullOrEmpty(PatientIdForPatientList) || 
                                      !string.IsNullOrEmpty(ExamIdForPatientList) || 
                                      !string.IsNullOrEmpty(OutpatientIdForPatientList);
            
            // 根据查询条件获取今日患者信息
            if (!string.IsNullOrEmpty(PatientIdForPatientList))
            {
                // 过滤患者列表
                TodayPatients = allPatients.Where(p => p.PatientId.Equals(PatientIdForPatientList, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (!string.IsNullOrEmpty(ExamIdForPatientList))
            {
                // 过滤患者列表
                TodayPatients = allPatients.Where(p => p.ExamId.Equals(ExamIdForPatientList, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (!string.IsNullOrEmpty(OutpatientIdForPatientList))
            {
                // 过滤患者列表
                TodayPatients = allPatients.Where(p => p.OutpatientId.Equals(OutpatientIdForPatientList, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                TodayPatients = allPatients;
            }

            return Page();
        }
    }
}