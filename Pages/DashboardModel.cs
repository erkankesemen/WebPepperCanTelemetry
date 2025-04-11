using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;
using WebPepperCan.Hubs;

namespace WebPepperCan.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<MessageHub> _hubContext;

        public Dictionary<string, string> VehicleStats { get; set; }
        public List<Vehicle> Vehicles { get; set; }

        public DashboardModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<MessageHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            // Kullanıcının organizasyonundaki araçları getir
            Vehicles = await _context.Vehicles
                .Include(v => v.TelemetryDevice)
                .Where(v => v.OrganizationId == user.OrganizationId)
                .ToListAsync();

            // Aktif/Pasif araç sayılarını hesapla
            int totalVehicles = Vehicles.Count;
            int activeVehicles = Vehicles.Count(v => v.AktifMi);
            int inactiveVehicles = totalVehicles - activeVehicles;

            // İstatistikleri hazırla
            VehicleStats = new Dictionary<string, string>
            {
                { "Total Vehicles", totalVehicles.ToString() },
                { "Active Vehicles", "0 %" },
                { "Inactive Vehicles", "100 %" },
                { "Critical Vehicles", "0 vehicles" },
                { "Critical Alerts", "N/A" },
                { "Changing Vehicles", "0 vehicles" },
                { "Vehicle Errors", "N/A" },
                { "CVCE Status", "N/A" }
            };

            return Page();
        }
    }
} 