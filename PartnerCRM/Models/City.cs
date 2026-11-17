
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        // Oficiální kód obce podle RÚIAN
        [StringLength(6)]
        public string? RuianMunicipalityCode { get; set; }

        [Required]
        public int CountryId { get; set; }

        public Country Country { get; set; } = null!;
    }
}
