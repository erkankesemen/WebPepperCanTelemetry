using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;
using WebPepperCan.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using WebPepperCan.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using WebPepperCan.Controllers;
using WebPepperCan.Models;

namespace WebPepperCan.Pages.Admin
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserActivityService _activityService;
        private readonly IUserSessionService _sessionService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, IUserActivityService activityService, IUserSessionService sessionService, ILogger<IndexModel> logger)
        {
            _context = context;
            _activityService = activityService;
            _sessionService = sessionService;
            _logger = logger;
            TelemetryDeviceModal = new TelemetryDeviceModalViewModel();
            
            // Tüm koleksiyonları başlat
            Users = new List<ApplicationUser>();
            Organizations = new List<Organization>();
            Vehicles = new List<Vehicle>();
            TelemetryDevices = new List<TelemetryDevice>();
            RecentActivities = new List<UserActivity>();
            SuspiciousActivities = new List<UserActivity>();
            ActiveSessions = new List<UserSession>();
            UnassignedVehicles = new List<Vehicle>();
        }

        [BindProperty]
        public ApplicationUser NewUser { get; set; }

        [BindProperty]
        public TelemetryDevice NewDevice { get; set; }

        public IList<ApplicationUser> Users { get; set; }
        public IList<Organization> Organizations { get; set; }
        public IEnumerable<UserActivity> RecentActivities { get; set; }
        public IEnumerable<UserActivity> SuspiciousActivities { get; set; }
        public int TotalActivitiesCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public IEnumerable<UserSession> ActiveSessions { get; set; }
        public IList<Vehicle> Vehicles { get; set; }
        public IList<TelemetryDevice> TelemetryDevices { get; set; }
        public TelemetryDeviceModalViewModel TelemetryDeviceModal { get; private set; }
        public List<Vehicle> UnassignedVehicles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await LoadData();

                var activitiesResult = await _activityService.GetActivitiesAsync(1, 10, null, null, null);
                RecentActivities = activitiesResult.Activities ?? new List<UserActivity>();
                TotalActivitiesCount = activitiesResult.TotalCount;
                
                SuspiciousActivities = await _activityService.GetSuspiciousActivities() ?? new List<UserActivity>();
                ActiveSessions = await _sessionService.GetActiveSessionsAsync() ?? new List<UserSession>();
                ActiveSessionCount = await _sessionService.GetActiveSessionCountAsync();

                Vehicles = await _context.Vehicles
                    .Include(v => v.Organization)
                    .Include(v => v.TelemetryDevice)
                    .OrderBy(v => v.Organization.KurumAdi)
                    .ThenBy(v => v.Plaka)
                    .ToListAsync();

                TelemetryDevices = await _context.TelemetryDevices
                    .Include(d => d.Organization)
                    .Include(d => d.Vehicle)
                    .OrderByDescending(d => d.LastCommunication)
                    .ToListAsync();

                UnassignedVehicles = await _context.Vehicles
                    .Where(v => v.TelemetryDevice == null || v.TelemetryDevice != null)
                    .OrderBy(v => v.Plaka)
                    .ToListAsync();

                TelemetryDeviceModal = new TelemetryDeviceModalViewModel
                {
                    Organizations = Organizations.ToList(),
                    Vehicles = Vehicles.Where(v => v.TelemetryDevice == null).ToList()
                };

                Organizations = await _context.Organizations.ToListAsync();
                Users = await _context.Users
                    .Include(u => u.Organization)
                    .ToListAsync();
                Vehicles = await _context.Vehicles
                    .Include(v => v.Organization)
                    .Include(v => v.TelemetryDevice)
                    .ToListAsync();
                TelemetryDevices = await _context.TelemetryDevices
                    .Include(d => d.Vehicle)
                    .Include(d => d.Organization)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                // Hata durumunda koleksiyonları boş liste olarak ayarla
                ResetCollections();
                ModelState.AddModelError("", $"Veriler yüklenirken bir hata oluştu: {ex.Message}");
                return Page();
            }
        }

        private void ResetCollections()
        {
            Users = new List<ApplicationUser>();
            Organizations = new List<Organization>();
            RecentActivities = new List<UserActivity>();
            ActiveSessions = new List<UserSession>();
            Vehicles = new List<Vehicle>();
            TelemetryDevices = new List<TelemetryDevice>();
            TelemetryDeviceModal = new TelemetryDeviceModalViewModel
            {
                Organizations = new List<Organization>(),
                Vehicles = new List<Vehicle>()
            };
        }

        private async Task LoadData()
        {
            Users = await _context.Users
                .Include(u => u.Organization)
                .ToListAsync() ?? new List<ApplicationUser>();

            Organizations = await _context.Organizations
                .ToListAsync() ?? new List<Organization>();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                if (NewUser == null)
                {
                    ModelState.AddModelError("", "Kullanıcı bilgileri boş olamaz.");
                    return Page();
                }

                await _context.Users.AddAsync(NewUser);
                await _context.SaveChangesAsync();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Kullanıcı eklenirken bir hata oluştu: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnGetActivityListAsync()
        {
            RecentActivities = await _activityService.GetRecentActivities(5);
            return Partial("_ActivityListPartial", RecentActivities);
        }

        public async Task<IActionResult> OnGetTelemetryDeviceModalAsync(int? id = null)
        {
            try
            {
                var viewModel = new TelemetryDeviceModalViewModel();

                viewModel.Organizations = await _context.Organizations
                    .OrderBy(o => o.KurumAdi)
                    .ToListAsync();

                viewModel.Vehicles = await _context.Vehicles
                    .OrderBy(v => v.Plaka)
                    .ToListAsync();

                if (id.HasValue)
                {
                    var device = await _context.TelemetryDevices
                        .Include(d => d.Organization)
                        .Include(d => d.Vehicle)
                        .FirstOrDefaultAsync(d => d.Id == id);

                    if (device != null)
                    {
                        viewModel.AktifMi = true;
                        viewModel.Device = device;
                    }
                }

                return Partial("_TelemetryDeviceModalPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazı modal yüklenirken hata oluştu");
                return StatusCode(500, "Bir hata oluştu");
            }
        }

        public async Task<IActionResult> OnGetTelemetryDeviceDetailsAsync(int id)
        {
            try
            {
                var device = await _context.TelemetryDevices
                    .Include(d => d.Organization)
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                    return NotFound();

                var viewModel = new TelemetryDeviceModalViewModel
                {
                    Device = device,
                    AktifMi = true,
                    Organizations = await _context.Organizations.ToListAsync(),
                    Vehicles = await _context.Vehicles.ToListAsync()
                };

                return Partial("_TelemetryDeviceDetailsPartial", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazı detayları yüklenirken hata oluştu");
                return StatusCode(500, "Bir hata oluştu");
            }
        }

        public async Task<IActionResult> OnPostAddTelemetryDeviceAsync(TelemetryDevice device)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                device.CreatedAt = DateTime.UtcNow;
                device.AktifMi = true;
                
                await _context.TelemetryDevices.AddAsync(device);
                await _context.SaveChangesAsync();

                // Başarılı işlem sonrası kullanıcı aktivitesi kaydet
                var userActivity = new UserActivity
                {
                    UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ActivityType = "TelemetryDeviceCreated",
                    Description = $"Yeni telemetri cihazı eklendi: {device.SerialNumber}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                await _context.UserActivities.AddAsync(userActivity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Telemetri cihazı başarıyla eklendi.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Telemetri cihazı eklenirken bir hata oluştu: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(VehiclesController.VehicleUpdateDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    TempData["ErrorMessage"] = string.Join(", ", errors);
                    return RedirectToPage();
                }

                var vehicle = await _context.Vehicles
                    .Include(v => v.Organization)
                    .FirstOrDefaultAsync(v => v.Id == model.Id);

                if (vehicle == null)
                {
                    TempData["ErrorMessage"] = "Araç bulunamadı.";
                    return RedirectToPage();
                }

                vehicle.Plaka = model.Plaka.ToUpper().Trim();
                vehicle.Sase = model.Sase.ToUpper().Trim();
                vehicle.OrganizationId = model.OrganizationId;
                vehicle.Location = model.Location?.Trim();
                vehicle.AktifMi = model.AktifMi;
                vehicle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Araç başarıyla güncellendi.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle");
                TempData["ErrorMessage"] = "Araç güncellenirken bir hata oluştu.";
                return RedirectToPage();
            }
        }
    }
}
