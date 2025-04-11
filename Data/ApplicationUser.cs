using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPepperCan.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public int? OrganizationId { get; set; }
        public bool AktifMi { get; set; } = true; // Varsayılan olarak aktif

        // Navigation properties
        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }
    }
}
