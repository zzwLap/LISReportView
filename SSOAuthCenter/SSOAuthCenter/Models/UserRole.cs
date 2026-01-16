using System.ComponentModel.DataAnnotations;

namespace SSOAuthCenter.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int RoleId { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        // 导航属性
        public virtual User? User { get; set; }
        public virtual Role? Role { get; set; }
    }
}