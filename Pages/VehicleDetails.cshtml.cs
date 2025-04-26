using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebPepperCan.Data;
using WebPepperCan.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using WebPepperCan.Hubs;
using Microsoft.Extensions.Logging;

namespace WebPepperCan.Pages
{
    public class VehicleDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<VehicleDetailsModel> _logger;
        private readonly IHubContext<MessageHub> _hubContext;

        public VehicleDetailsModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<VehicleDetailsModel> logger,
            IHubContext<MessageHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _hubContext = hubContext;
            Vehicles = new List<VehicleListItemViewModel>();
        }

        public VehicleDetailsViewModel VehicleDetails { get; set; }
        public VehiclePopupViewModel PopupData { get; set; }
        public List<VehicleListItemViewModel> Vehicles { get; private set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Kullanıcı bulunamadı");
                return RedirectToPage("/Account/Login");
            }

            var vehicles = await _context.Vehicles
                .Include(v => v.TelemetryDevice)
                .Where(v => v.OrganizationId == user.OrganizationId)
                .ToListAsync();

            Vehicles.Clear();
            foreach (var v in vehicles)
            {
                var signalRData = await GetLastSignalRData(v.Id);
                
                var latitude = signalRData?.Latitude ?? 
                              (!string.IsNullOrEmpty(v.LastLatitude) ? v.LastLatitude : "39.793629");
                var longitude = signalRData?.Longitude ?? 
                              (!string.IsNullOrEmpty(v.LastLongitude) ? v.LastLongitude : "32.432211");

                Vehicles.Add(new VehicleListItemViewModel
                {
                    Id = v.Id,
                    DisplayName = $"{v.Plaka} / {v.TelemetryDevice.SerialNumber}",
                    IsActive = v.TelemetryDevice.LastCommunication > DateTime.Now.AddMinutes(-5),
                    Latitude = latitude,
                    Longitude = longitude
                });
            }

            if (!vehicles.Any())
            {
                _logger.LogInformation($"Kullanıcının ({user.Id}) organizasyonunda araç bulunamadı");
                VehicleDetails = null;
                PopupData = null;
                return Page();
            }

            var vehicle = id.HasValue 
                ? vehicles.FirstOrDefault(v => v.Id == id.Value)
                : vehicles.FirstOrDefault();

            if (vehicle != null)
            {
                VehicleDetails = new VehicleDetailsViewModel
                {
                    Id = vehicle.Id,
                    Location = vehicle.Location ?? "Konum bilgisi yok",
                    LastActive = vehicle.TelemetryDevice.LastCommunication?.ToString("dd.MM.yyyy HH:mm:ss") ?? "n/a",
                     DisplayName = $"{vehicle.Plaka} / {vehicle.TelemetryDevice?.SerialNumber}",
                    VehicleSpeed = "0 km/h",
                    EngineSpeed = "n/a",
                    FuelLevel = "n/a",
                    EngineCoolant = "17°C",
                    Telltales = "OK"
                };

                PopupData = new VehiclePopupViewModel
                {
                    Title = vehicle.Plaka,
                    Subtitle = vehicle.TelemetryDevice.SerialNumber,
                    Location = vehicle.Location ?? "Konum bilgisi yok",
                    LastActive = vehicle.TelemetryDevice.LastCommunication?.ToString("dd.MM.yyyy HH:mm:ss") ?? "n/a"
                };
            }

            return Page();
        }

        private async Task<VehicleSignalRData> GetLastSignalRData(int vehicleId)
        {
            try
            {
                var cacheKey = $"Vehicle_{vehicleId}_LastData";
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Araç {vehicleId} için SignalR verisi alınırken hata oluştu");
                return null;
            }
        }
    }

    public class VehiclePopupViewModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Location { get; set; }
        public string LastActive { get; set; }
    }

    public class VehicleListItemViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }

    public class VehicleSignalRData
    {
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 