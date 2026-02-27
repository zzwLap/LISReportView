using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LisReportServer.Models
{
    /// <summary>
    /// 医院服务配置
    /// </summary>
    public class HospitalServiceConfig
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "医院ID不能为空")]
        [Display(Name = "医院ID")]
        public int HospitalProfileId { get; set; }

        [Required(ErrorMessage = "服务名称不能为空")]
        [StringLength(200, ErrorMessage = "服务名称不能超过200个字符")]
        [Display(Name = "服务名称")]
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "服务类别不能为空")]
        [StringLength(100, ErrorMessage = "服务类别不能超过100个字符")]
        [Display(Name = "服务类别")]
        public string ServiceCategory { get; set; } = string.Empty;

        [Required(ErrorMessage = "服务发现键值不能为空")]
        [StringLength(200, ErrorMessage = "服务发现键值不能超过200个字符")]
        [Display(Name = "服务发现键值")]
        public string ServiceDiscoveryKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "服务地址不能为空")]
        [StringLength(500, ErrorMessage = "服务地址不能超过500个字符")]
        [Display(Name = "服务地址")]
        public string ServiceAddress { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "端口号必须在1-65535之间")]
        [Display(Name = "服务端口")]
        public int? ServicePort { get; set; }

        [StringLength(20, ErrorMessage = "API版本号不能超过20个字符")]
        [Display(Name = "API版本")]
        public string? ApiVersion { get; set; }

        [StringLength(50, ErrorMessage = "认证类型不能超过50个字符")]
        [Display(Name = "认证类型")]
        public string? AuthType { get; set; }

        [StringLength(100, ErrorMessage = "用户名不能超过100个字符")]
        [Display(Name = "用户名")]
        public string? Username { get; set; }

        [StringLength(500, ErrorMessage = "密码不能超过500个字符")]
        [Display(Name = "加密密码")]
        public string? EncryptedPassword { get; set; }

        [StringLength(500, ErrorMessage = "API密钥不能超过500个字符")]
        [Display(Name = "API密钥")]
        public string? ApiKey { get; set; }

        [Range(1, 300, ErrorMessage = "超时时间必须在1-300秒之间")]
        [Display(Name = "超时时间(秒)")]
        public int? Timeout { get; set; }

        [Range(0, 10, ErrorMessage = "重试次数必须在0-10之间")]
        [Display(Name = "重试次数")]
        public int? RetryCount { get; set; }

        [StringLength(500, ErrorMessage = "健康检查地址不能超过500个字符")]
        [Display(Name = "健康检查地址")]
        public string? HealthCheckUrl { get; set; }

        [Display(Name = "是否启用")]
        public bool IsActive { get; set; } = true;

        [Range(0, 100, ErrorMessage = "优先级必须在0-100之间")]
        [Display(Name = "优先级")]
        public int Priority { get; set; } = 50;

        [StringLength(1000, ErrorMessage = "服务描述不能超过1000个字符")]
        [Display(Name = "服务描述")]
        public string? Description { get; set; }

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 导航属性：关联医院基本信息
        [ForeignKey("HospitalProfileId")]
        public virtual HospitalProfile? HospitalProfile { get; set; }
    }
}
