using System.Collections.Concurrent;

namespace LisReportServer.Services
{
    public class ReportSummary
    {
        public int TotalReports { get; set; }
        public int SuccessfulReports { get; set; }
        public int FailedReports { get; set; }
        public DateTime Date { get; set; }
    }

    public class PatientInfo
    {
        public string PatientId { get; set; } = string.Empty; // 住院号
        public string ExamId { get; set; } = string.Empty;    // 检查号
        public string OutpatientId { get; set; } = string.Empty; // 门诊号
        public string PatientName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public DateTime ExamTime { get; set; }
        public string ExamStatus { get; set; } = "待检查"; // 待检查, 已检查
        public string ReportStatus { get; set; } = "未上传"; // 未上传, 已上传, 解析成功, 解析失败
        public string? ErrorMessage { get; set; }
    }

    public class ReportRecord
    {
        public string ReportId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty; // 住院号
        public string ExamId { get; set; } = string.Empty;    // 检查号
        public string OutpatientId { get; set; } = string.Empty; // 门诊号
        public string PatientName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IReportService
    {
        Task<ReportSummary> GetTodaySummaryAsync();
        Task<List<ReportRecord>> GetReportsByPatientIdAsync(string patientId);
        Task<List<ReportRecord>> GetReportsByExamIdAsync(string examId);
        Task<List<ReportRecord>> GetReportsByOutpatientIdAsync(string outpatientId);
        Task<List<ReportRecord>> GetAllTodayReportsAsync();
        Task<bool> AddReportRecordAsync(ReportRecord record);
        Task<List<PatientInfo>> GetTodayPatientsAsync();
        Task<bool> UpdatePatientReportStatusAsync(string examId, string reportStatus, string? errorMessage = null);
    }

    public class ReportService : IReportService
    {
        // 使用内存存储模拟数据，实际应用中应使用数据库
        private static readonly ConcurrentDictionary<string, ReportRecord> _reports = new();
        private static readonly ConcurrentDictionary<string, PatientInfo> _patients = new();
        
        // 初始化一些示例数据
        static ReportService()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            
            // 添加一些示例报告数据
            _reports.TryAdd("R001", new ReportRecord
            {
                ReportId = "R001",
                PatientId = "P001",
                ExamId = "E001",
                OutpatientId = "O001",
                PatientName = "张三",
                ExamType = "血常规",
                UploadTime = today.AddHours(9).AddMinutes(15),
                IsSuccessful = true
            });
            
            _reports.TryAdd("R002", new ReportRecord
            {
                ReportId = "R002",
                PatientId = "P002",
                ExamId = "E002",
                OutpatientId = "O002",
                PatientName = "李四",
                ExamType = "尿常规",
                UploadTime = today.AddHours(10).AddMinutes(30),
                IsSuccessful = true
            });
            
            _reports.TryAdd("R003", new ReportRecord
            {
                ReportId = "R003",
                PatientId = "P003",
                ExamId = "E003",
                OutpatientId = "O003",
                PatientName = "王五",
                ExamType = "肝功能",
                UploadTime = today.AddHours(11).AddMinutes(45),
                IsSuccessful = false,
                ErrorMessage = "数据格式错误"
            });
            
            _reports.TryAdd("R004", new ReportRecord
            {
                ReportId = "R004",
                PatientId = "P004",
                ExamId = "E004",
                OutpatientId = "O004",
                PatientName = "赵六",
                ExamType = "肾功能",
                UploadTime = today.AddHours(14).AddMinutes(20),
                IsSuccessful = true
            });
            
            // 添加一些示例患者数据
            _patients.TryAdd("E001", new PatientInfo
            {
                PatientId = "P001",
                ExamId = "E001",
                OutpatientId = "O001",
                PatientName = "张三",
                ExamType = "血常规",
                ExamTime = today.AddHours(9).AddMinutes(0),
                ExamStatus = "已检查",
                ReportStatus = "解析成功"
            });
            
            _patients.TryAdd("E002", new PatientInfo
            {
                PatientId = "P002",
                ExamId = "E002",
                OutpatientId = "O002",
                PatientName = "李四",
                ExamType = "尿常规",
                ExamTime = today.AddHours(10).AddMinutes(0),
                ExamStatus = "已检查",
                ReportStatus = "解析成功"
            });
            
            _patients.TryAdd("E003", new PatientInfo
            {
                PatientId = "P003",
                ExamId = "E003",
                OutpatientId = "O003",
                PatientName = "王五",
                ExamType = "肝功能",
                ExamTime = today.AddHours(11).AddMinutes(0),
                ExamStatus = "已检查",
                ReportStatus = "解析失败",
                ErrorMessage = "数据格式错误"
            });
            
            _patients.TryAdd("E004", new PatientInfo
            {
                PatientId = "P004",
                ExamId = "E004",
                OutpatientId = "O004",
                PatientName = "赵六",
                ExamType = "肾功能",
                ExamTime = today.AddHours(14).AddMinutes(0),
                ExamStatus = "待检查",
                ReportStatus = "未上传"
            });
            
            _patients.TryAdd("E005", new PatientInfo
            {
                PatientId = "P005",
                ExamId = "E005",
                OutpatientId = "O005",
                PatientName = "钱七",
                ExamType = "心电图",
                ExamTime = today.AddHours(15).AddMinutes(0),
                ExamStatus = "待检查",
                ReportStatus = "未上传"
            });
        }

        public async Task<ReportSummary> GetTodaySummaryAsync()
        {
            var today = DateTime.Today;
            var todayReports = _reports.Values
                .Where(r => r.UploadTime.Date == today)
                .ToList();

            var summary = new ReportSummary
            {
                Date = today,
                TotalReports = todayReports.Count,
                SuccessfulReports = todayReports.Count(r => r.IsSuccessful),
                FailedReports = todayReports.Count(r => !r.IsSuccessful)
            };

            return await Task.FromResult(summary);
        }

        public async Task<List<ReportRecord>> GetReportsByPatientIdAsync(string patientId)
        {
            var today = DateTime.Today;
            var reports = _reports.Values
                .Where(r => r.PatientId.Equals(patientId, StringComparison.OrdinalIgnoreCase) 
                           && r.UploadTime.Date == today)
                .OrderByDescending(r => r.UploadTime)
                .ToList();

            return await Task.FromResult(reports);
        }

        public async Task<List<ReportRecord>> GetReportsByExamIdAsync(string examId)
        {
            var today = DateTime.Today;
            var reports = _reports.Values
                .Where(r => r.ExamId.Equals(examId, StringComparison.OrdinalIgnoreCase) 
                           && r.UploadTime.Date == today)
                .OrderByDescending(r => r.UploadTime)
                .ToList();

            return await Task.FromResult(reports);
        }

        public async Task<List<ReportRecord>> GetReportsByOutpatientIdAsync(string outpatientId)
        {
            var today = DateTime.Today;
            var reports = _reports.Values
                .Where(r => r.OutpatientId.Equals(outpatientId, StringComparison.OrdinalIgnoreCase) 
                           && r.UploadTime.Date == today)
                .OrderByDescending(r => r.UploadTime)
                .ToList();

            return await Task.FromResult(reports);
        }

        public async Task<List<ReportRecord>> GetAllTodayReportsAsync()
        {
            var today = DateTime.Today;
            var reports = _reports.Values
                .Where(r => r.UploadTime.Date == today)
                .OrderByDescending(r => r.UploadTime)
                .ToList();

            return await Task.FromResult(reports);
        }

        public async Task<bool> AddReportRecordAsync(ReportRecord record)
        {
            record.UploadTime = DateTime.Now;
            _reports.TryAdd(record.ReportId, record);
            return await Task.FromResult(true);
        }

        public async Task<List<PatientInfo>> GetTodayPatientsAsync()
        {
            var today = DateTime.Today;
            var patients = _patients.Values
                .Where(p => p.ExamTime.Date == today)
                .OrderBy(p => p.ExamTime)
                .ToList();

            return await Task.FromResult(patients);
        }

        public async Task<bool> UpdatePatientReportStatusAsync(string examId, string reportStatus, string? errorMessage = null)
        {
            if (_patients.TryGetValue(examId, out var patient))
            {
                patient.ReportStatus = reportStatus;
                patient.ErrorMessage = errorMessage;
                
                // 如果报告已上传或解析成功，更新检查状态为已检查
                if (reportStatus == "已上传" || reportStatus == "解析成功")
                {
                    patient.ExamStatus = "已检查";
                }
                
                return true;
            }
            
            return false;
        }
    }
}