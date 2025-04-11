using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;
using Microsoft.AspNetCore.Authorization;

namespace WebPepperCan.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class VehicleManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VehicleManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Vehicle> Vehicles { get; set; } = new();
        public List<Organization> Organizations { get; set; } = new();

        public async Task OnGetAsync()
        {
            Vehicles = await _context.Vehicles
                .Include(v => v.Organization)
                .Include(v => v.TelemetryDevice)
                .OrderBy(v => v.Organization.KurumAdi)
                .ThenBy(v => v.Plaka)
                .ToListAsync();

            Organizations = await _context.Organizations
                .OrderBy(o => o.KurumAdi)
                .ToListAsync();
        }
    }
} 