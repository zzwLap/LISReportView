using System.ComponentModel.DataAnnotations;

namespace SSOAuthCenter.Models
{
    public class ClientApplication
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ClientId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string ClientSecret { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string ClientName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string RedirectUri { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? LogoutRedirectUri { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}