using Microsoft.AspNetCore.Mvc.RazorPages;
using WebPepperCan.Data;
using WebPepperCan.Models;
using Microsoft.EntityFrameworkCore;

namespace WebPepperCan.Pages.Admin
{
    public class TelemetryDevicesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TelemetryDevicesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TelemetryDevice> TelemetryDevices { get; set; } = new();
        public List<Organization> Organizations { get; set; } = new();
        public List<Vehicle> UnassignedVehicles { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Telemetri cihazlarını, organizasyonları ve araçları yükle
            TelemetryDevices = await _context.TelemetryDevices
                .Include(d => d.Organization)
                .Include(d => d.Vehicle)
                .OrderByDescending(d => d.LastCommunication)
                .ToListAsync();

            Organizations = await _context.Organizations
                .OrderBy(o => o.KurumAdi)
                .ToListAsync();

            // Henüz telemetri cihazı atanmamış araçları getir
            UnassignedVehicles = await _context.Vehicles
                .Where(v => v.TelemetryDevice == null && v.AktifMi)
                .OrderBy(v => v.Plaka)
                .ToListAsync();
        }
    }
} 