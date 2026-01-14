using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Models
{
    public class HospitalServerConfig
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "医院名称不能为空")]
        [StringLength(200, ErrorMessage = "医院名称不能超过200个字符")]
        public string HospitalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "医院编码不能为空")]
        [StringLength(50, ErrorMessage = "医院编码不能超过50个字符")]
        [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "医院编码只能包含字母、数字、下划线和横线")]
        public string HospitalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "服务器地址不能为空")]
        [StringLength(500, ErrorMessage = "服务器地址不能超过500个字符")]
        public string ServerAddress { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "端口号必须在1-65535之间")]
        public int? Port { get; set; }

        [StringLength(100, ErrorMessage = "用户名不能超过100个字符")]
        public string? Username { get; set; }

        [StringLength(500, ErrorMessage = "密码不能超过500个字符")]
        public string? EncryptedPassword { get; set; }

        [StringLength(1000, ErrorMessage = "其他参数不能超过1000个字符")]
        public string? OtherParameters { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}