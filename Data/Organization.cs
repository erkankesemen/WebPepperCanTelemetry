using System;
using System.Collections.Generic;

namespace WebPepperCan.Data
{
    public class Organization
    {
        public int Id { get; set; }
        public string KurumAdi { get; set; }
        public string Adres { get; set; }
        public string Tel { get; set; }
        public string Email { get; set; }
        public string IlgiliKisi { get; set; }
        public bool AktifMi { get; set; } = true;
        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<ApplicationUser> Users { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; }
        public virtual ICollection<TelemetryDevice> TelemetryDevices { get; set; }

        public Organization()
        {
            Users = new HashSet<ApplicationUser>();
            Vehicles = new HashSet<Vehicle>();
            TelemetryDevices = new HashSet<TelemetryDevice>();
        }
      
    }
}