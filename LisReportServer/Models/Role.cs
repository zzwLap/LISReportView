using System.ComponentModel.DataAnnotations;

namespace LisReportServer.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "角色名称不能为空")]
        [StringLength(50, ErrorMessage = "角色名称不能超过50个字符")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "角色描述不能超过200个字符")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}