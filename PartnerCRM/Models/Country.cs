
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2)]
        public string Code2 { get; set; } = string.Empty;

        [StringLength(3)]
        public string Code3 { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public ICollection<City> Cities { get; set; }
            = new List<City>();
    }
}
