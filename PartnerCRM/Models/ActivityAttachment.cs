using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PartnerCRM.Models
{
    public class ActivityAttachment
    {
        public int Id { get; set; }

        [Required]
        public int ActivityId { get; set; }

        [ValidateNever]
        public Activity Activity { get; set; } = null!;

        // Původní název souboru
        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        // Interní název uloženého souboru
        [Required]
        [StringLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        // Typ souboru
        [StringLength(150)]
        public string? ContentType { get; set; }

        // Velikost souboru v bajtech
        public long FileSize { get; set; }

        // Datum nahrání
        public DateTime UploadedAt { get; set; }
            = DateTime.Now;
    }
}