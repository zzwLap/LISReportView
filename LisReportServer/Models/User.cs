using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, ErrorMessage = "用户名不能超过50个字符")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(255, ErrorMessage = "密码不能超过255个字符")]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "邮箱不能超过100个字符")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        public string? Email { get; set; }

        [StringLength(100, ErrorMessage = "姓名不能超过100个字符")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "医院名称不能为空")]
        [StringLength(200, ErrorMessage = "医院名称不能超过200个字符")]
        public string HospitalName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        //导航属性
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}