using System.ComponentModel.DataAnnotations;

namespace SSOAuthCenter.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? PasswordHash { get; set; }
        
        [StringLength(100)]
        public string? FirstName { get; set; }
        
        [StringLength(100)]
        public string? LastName { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsEmailConfirmed { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // 导航属性
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}