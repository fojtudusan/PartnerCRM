
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class Bank
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(4)]
        public string BankCode { get; set; } = string.Empty;
    }
}
