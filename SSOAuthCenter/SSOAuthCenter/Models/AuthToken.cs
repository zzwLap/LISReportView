using System.ComponentModel.DataAnnotations;

namespace SSOAuthCenter.Models
{
    public class AuthToken
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(500)]
        public string TokenValue { get; set; } = string.Empty;
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TokenType { get; set; } = string.Empty; // Access, Refresh, Authorization code
        
        [StringLength(100)]
        public string? ClientId { get; set; }
        
        [StringLength(500)]
        public string? Scopes { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRevoked { get; set; } = false;
        
        public DateTime? RevokedAt { get; set; }
        
        // 导航属性
        public virtual User? User { get; set; }
    }
}