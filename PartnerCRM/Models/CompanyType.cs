
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class CompanyType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Company> Companies { get; set; }
            = new List<Company>();
    }
}
