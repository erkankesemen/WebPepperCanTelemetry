using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPepperCan.Data
{
    public class TelemetryDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SerialNumber { get; set; }  // Cihaz Seri Numarası  

        [Required]
        [MaxLength(20)]
        public string SimCardNumber { get; set; }  // SIM Kart Numarası

        [MaxLength(20)]
        public string FirmwareVersion { get; set; }  // Yazılım Versiyonu

        public DateTime? InstallationDate { get; set; }  // Kurulum Tarihi

        public DateTime? LastCommunication { get; set; }  // Son İletişim Zamanı

        public bool AktifMi { get; set; } = true;

        [MaxLength(500)]
        public string? Notes { get; set; }  // Ek Notlar

        // Vehicle ile ilişki (1-1) - Nullable yap
        public int? VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

 
        public int OrganizationId { get; set; }
        [ForeignKey("OrganizationId")]

        public virtual Organization Organization { get; set; }

        // Telemetri durumu
        public bool IsOnline { get; set; } = false;
        public string? LastKnownLocation { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<VehicleSignal> VehicleSignals { get; set; }
    
    
    }
} 