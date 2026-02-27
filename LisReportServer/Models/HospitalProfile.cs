using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Models
{
    /// <summary>
    /// 医院基本信息配置
    /// </summary>
    public class HospitalProfile
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "医院名称不能为空")]
        [StringLength(200, ErrorMessage = "医院名称不能超过200个字符")]
        [Display(Name = "医院名称")]
        public string HospitalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "医院编码不能为空")]
        [StringLength(50, ErrorMessage = "医院编码不能超过50个字符")]
        [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "医院编码只能包含字母、数字、下划线和横线")]
        [Display(Name = "医院编码")]
        public string HospitalCode { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "医院简称不能超过100个字符")]
        [Display(Name = "医院简称")]
        public string? ShortName { get; set; }

        [StringLength(500, ErrorMessage = "医院地址不能超过500个字符")]
        [Display(Name = "医院地址")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "联系电话不能超过50个字符")]
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        [Display(Name = "联系电话")]
        public string? ContactPhone { get; set; }

        [StringLength(100, ErrorMessage = "联系邮箱不能超过100个字符")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        [Display(Name = "联系邮箱")]
        public string? ContactEmail { get; set; }

        [StringLength(1000, ErrorMessage = "医院描述不能超过1000个字符")]
        [Display(Name = "医院描述")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Logo路径不能超过500个字符")]
        [Display(Name = "Logo路径")]
        public string? Logo { get; set; }

        [Display(Name = "是否启用")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 导航属性：一个医院可以有多个服务配置
        public virtual ICollection<HospitalServiceConfig> ServiceConfigs { get; set; } = new List<HospitalServiceConfig>();
    }
}
