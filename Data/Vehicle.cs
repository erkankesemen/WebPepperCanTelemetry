using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPepperCan.Data
{
    public class Vehicle
    {

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Plaka { get; set; }
        [Required]
        [StringLength(50)]
        
        public string Sase { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }
        public string? LastLatitude { get; set; }
        public string? LastLongitude { get; set; }
        public DateTime? LastActive { get; set; }
        // Organization ile ilişki
        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        // Telemetri cihazı ile ilişki (1-1)
        public virtual TelemetryDevice TelemetryDevice { get; set; }

        // Opsiyonel: Oluşturma ve güncelleme bilgileri
        public bool AktifMi { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public void UpdateOrganizationId(int organizationId) {
            this.OrganizationId = organizationId;
            if (this.TelemetryDevice != null) {
                this.TelemetryDevice.OrganizationId = organizationId;
            }
        }
    }
}