
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class MarketingRecipient
    {
        public int Id { get; set; }

        // Vazba na marketingovou akci
        public int MarketingCampaignId { get; set; }

        public MarketingCampaign MarketingCampaign { get; set; }
            = null!;

        // Firma, v jejímž kontextu kontakt oslovujeme
        public int CompanyId { get; set; }

        public Company Company { get; set; } = null!;

        // Konkrétní kontakt
        public int ContactId { get; set; }

        public Contact Contact { get; set; } = null!;

        // Kopie e-mailové adresy použitá při rozesílání
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string EmailAddress { get; set; } = string.Empty;

        // Stav odeslání
        [StringLength(30)]
        public string Status { get; set; } = "Připraveno";

        public DateTime? SentAt { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
    }
}
