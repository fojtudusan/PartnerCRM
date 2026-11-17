
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class MarketingCampaign
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        // Vybraná e-mailová šablona
        public int? EmailTemplateId { get; set; }

        public EmailTemplate? EmailTemplate { get; set; }

        // Stav marketingové akce
        [StringLength(30)]
        public string Status { get; set; } = "Příprava";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? SentAt { get; set; }

        public ICollection<MarketingRecipient> Recipients { get; set; }
            = new List<MarketingRecipient>();
    }
}
