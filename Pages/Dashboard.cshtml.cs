using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebPepperCan.Data;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Dictionary<string, string> VehicleStats { get; set; }
    public List<VehicleData> Vehicles { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return;

        var vehicles = await _context.Vehicles
            .Include(v => v.TelemetryDevice)
            .Where(v => v.OrganizationId == user.OrganizationId)
            .ToListAsync();

        var activeVehicles = vehicles.Count(v => v.AktifMi && v.TelemetryDevice.IsOnline);

        VehicleStats = new Dictionary<string, string>
        {
            { "Total Vehicles", "1" },
            { "Active Vehicles", "100 %" },
            { "Inactive Vehicles", "0 %" },
            { "Critical Vehicles", "0 vehicles" },
            { "Critical Alerts", "N/A" },
            { "Changing Vehicles", "0 vehicles" },
            { "Vehicle Errors", "N/A" },
            { "CVCE Status", "N/A" }
        };

        Vehicles = vehicles.Select(v => new VehicleData
        {
            Id = $"{v.Plaka} / {v.TelemetryDevice.SerialNumber}",
            LastActive = v.TelemetryDevice.LastCommunication?.ToString("dd.MM.yyyy HH:mm:ss") ?? "n/a",
            Speed = "0 km/h",
            Mileage = "n/a",
            FuelUsed = "n/a",
            Propulsion = "Hybrid Diesel/Electric",
            Coolant = "9 °C",
            FuelLevel = "n/a",
            SoC = "n/a",
            ChargeType = "n/a"
        }).ToList();
    }
}

public class VehicleData
{
    public string Id { get; set; }
    public string LastActive { get; set; }
    public string Speed { get; set; }
    public string Mileage { get; set; }
    public string FuelUsed { get; set; }
    public string Propulsion { get; set; }
    public string Coolant { get; set; }
    public string FuelLevel { get; set; }
    public string SoC { get; set; }
    public string ChargeType { get; set; }
} 