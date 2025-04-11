using Microsoft.AspNetCore.Mvc.RazorPages;
using WebPepperCan.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace WebPepperCan.Pages.Shared.FleetListPartial
{
    public class FleetListPartialModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FleetListPartialModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class VehicleViewModel
        {
            public int Id { get; set; }
            public string Plaka { get; set; }
            public string SerialNumber { get; set; }
            public bool IsOnline { get; set; }
            public string? Location { get; set; }
            public DateTime? LastActive { get; set; }
        }

        public List<VehicleViewModel> Vehicles { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                Vehicles = await _context.Vehicles
                    .Include(v => v.TelemetryDevice)
                    .Where(v => v.OrganizationId == user.OrganizationId && v.AktifMi)
                    .Select(v => new VehicleViewModel
                    {
                        Id = v.Id,
                        Plaka = v.Plaka,
                        SerialNumber = v.TelemetryDevice.SerialNumber,
                        IsOnline = v.TelemetryDevice.IsOnline,
                        Location = v.Location,
                        LastActive = v.TelemetryDevice.LastCommunication
                    })
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnGetVehicleDetailsAsync(int vehicleId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var vehicle = await _context.Vehicles
                .Include(v => v.TelemetryDevice)
                .FirstOrDefaultAsync(v => 
                    v.Id == vehicleId && 
                    v.OrganizationId == user.OrganizationId && 
                    v.AktifMi);

            if (vehicle == null) return NotFound();

            return new JsonResult(new
            {
                vehicle.Plaka,
                vehicle.TelemetryDevice.SerialNumber,
                vehicle.Location,
                LastActive = vehicle.TelemetryDevice.LastCommunication,
                vehicle.TelemetryDevice.IsOnline
            });
        }

        public async Task<IActionResult> OnGetVehicleLocationsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var vehicles = await _context.Vehicles
                .Include(v => v.TelemetryDevice)
                .Where(v => v.OrganizationId == user.OrganizationId && v.AktifMi)
                .Select(v => new { v.Id, v.Plaka, v.TelemetryDevice.SerialNumber, v.Location, v.TelemetryDevice.IsOnline })
                .ToListAsync();

            var result = vehicles.Select(v => new
            {
                v.Id,
                v.Plaka,
                v.SerialNumber,
                v.Location,
                v.IsOnline,
                Latitude = v.Location != null ? double.Parse(v.Location.Split(',')[0]) : 0,
                Longitude = v.Location != null ? double.Parse(v.Location.Split(',')[1]) : 0
            });

            return new JsonResult(result);
        }
    }
} 