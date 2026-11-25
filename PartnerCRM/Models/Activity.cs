
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PartnerCRM.Models
{
    public class Activity
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        public int? ContactId { get; set; }
        // Vazba na marketingovou kampaň
        public int? MarketingCampaignId { get; set; }

        [ValidateNever]
        public MarketingCampaign? MarketingCampaign { get; set; }

        // Hierarchie aktivit
        public int? ParentActivityId { get; set; }

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; }
            = DateTime.Now;

        [StringLength(3000)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? Result { get; set; }

        [ValidateNever]
        public Company Company { get; set; } = null!;

        public Contact? Contact { get; set; }

        // Nadřazená aktivita
        [ValidateNever]
        public Activity? ParentActivity { get; set; }

        // Podaktivity
        [ValidateNever]
        public ICollection<Activity> ChildActivities { get; set; }
            = new List<Activity>();
        // Přílohy aktivity
        [ValidateNever]
        public ICollection<ActivityAttachment> Attachments { get; set; }
            = new List<ActivityAttachment>();
        // Úkol, ke kterému aktivita patří
        public int? TaskId { get; set; }

        [ValidateNever]
        public CrmTask? Task { get; set; }
    }
}
