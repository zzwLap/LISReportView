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

        public ReportSummary Summary { get; set; }
        public List<ReportRecord> Reports { get; set; }

        public async Task<ActionResult> OnGetAsync()
        {
            // 获取今日统计信息
            Summary = await _reportService.GetTodaySummaryAsync();

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
                Reports = await _reportService.GetAllTodayReportsAsync();
            }

            return Page();
        }
    }
}