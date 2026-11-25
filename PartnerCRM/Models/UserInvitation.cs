
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class UserInvitation
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string Role { get; set; } = "";

        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }
    }
}
