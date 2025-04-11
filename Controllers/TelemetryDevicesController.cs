using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using WebPepperCan.Data;
using WebPepperCan.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WebPepperCan.Models.ViewModels;

namespace WebPepperCan.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class TelemetryDevicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TelemetryDevicesController> _logger;
         private readonly IActivityService _activityService;


        public TelemetryDevicesController(
            ApplicationDbContext context, ILogger<TelemetryDevicesController> logger, IActivityService activityService)
        {
            _context = context;
            _logger = logger;
            _activityService = activityService;
        }


        [HttpGet]
        [Route("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var device = await _context.TelemetryDevices
                    .Include(d => d.Organization)
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                {
                    return Json(new { success = false, message = "Telemetri cihazı bulunamadı" });
                }

                // AktifMi değerini özellikle belirtelim
                var data = new {
                    id = device.Id,
                    serialNumber = device.SerialNumber,
                    simCardNumber = device.SimCardNumber,
                    vehicleId = device.VehicleId,
                    organizationId = device.OrganizationId,
                    firmwareVersion = device.FirmwareVersion,
                    installationDate = device.InstallationDate?.ToString("yyyy-MM-dd"),
                    notes = device.Notes,
                    aktifMi = device.AktifMi, // Bu değerin doğru geldiğinden emin olalım
                    organizationName = device.Organization?.KurumAdi,
                    vehiclePlate = device.Vehicle?.Plaka
                };

                _logger.LogInformation("Gönderilen veri: {@Data}", data); // Log ekleyelim

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazı düzenleme formu getirilirken hata oluştu");
                return Json(new { success = false, message = "Telemetri cihazı bilgileri alınamadı" });
            }
        }


        [HttpPost]
        [Route("Create", Name = "CreateTelemetryDevice")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] TelemetryDeviceCreateDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning("Model validation failed: {@Errors}", errors);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var telemetryDevice = new TelemetryDevice
                {
                    SerialNumber = model.SerialNumber.Trim(),
                    SimCardNumber = model.SimCardNumber.Trim(),
                    VehicleId = model.VehicleId,
                    OrganizationId = model.OrganizationId,
                    FirmwareVersion = model.FirmwareVersion?.Trim(),
                    InstallationDate = model.InstallationDate,
                    Notes = model.Notes?.Trim(),
                    AktifMi = model.AktifMi,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TelemetryDevices.Add(telemetryDevice);
                await _context.SaveChangesAsync();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(userId, "TelemetryDeviceCreated", 
                    $"Yeni Telemetri Cihazı Eklendi: {telemetryDevice.SerialNumber}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString()
                );

                return Json(new { success = true, message = "Telemetri cihazı başarıyla oluşturuldu" });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazı oluşturulurken hata oluştu: {Message}", ex.Message);
                return Json(new { success = false, message = "Telemetri cihazı oluşturulamadı", error = ex.Message });
            }
        }

      
        [HttpPost]
        [Route("Edit", Name = "EditTelemetryDevice")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] TelemetryDeviceUpdateDto model)
        {
            try
            {
                // Model doğrulama kontrolü
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Geçersiz form verisi" });
                }

                var device = await _context.TelemetryDevices.FindAsync(model.Id);
                if (device == null)
                {
                    return Json(new { success = false, message = "Telemetri cihazı bulunamadı" });
                }

                // Gelen veriyi logla
                _logger.LogInformation("Updating device: {@Model}", model);
                
                device.SerialNumber = model.SerialNumber.Trim();
                device.SimCardNumber = model.SimCardNumber.Trim();
                device.VehicleId = model.VehicleId;
                device.OrganizationId = model.OrganizationId;
                device.FirmwareVersion = model.FirmwareVersion?.Trim();
                device.InstallationDate = model.InstallationDate;
                device.Notes = model.Notes?.Trim();
                device.AktifMi = model.AktifMi;
                device.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Veritabanı güncelleme hatası");
                    return Json(new { success = false, message = "Veritabanı güncelleme hatası oluştu" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(userId, "TelemetryDeviceUpdated", 
                    $"Telemetri cihazı güncellendi: {device.SerialNumber}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message = "Telemetri cihazı başarıyla güncellendi" });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazı güncellenirken hata oluştu");
                return StatusCode(500, Json(new { success = false, message = "Güncelleme sırasında bir hata oluştu.", error = ex.Message }));
            }
        }

        [HttpGet]
        [Route("GetTable", Name = "GetTelemetryDevicesTable")]
        public async Task<IActionResult> GetTable()
        {
            try
            {
                var devices = await _context.TelemetryDevices
                    .Include(d => d.Organization)
                    .Include(d => d.Vehicle)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();

                return PartialView("_TelemetryDevicesTablePartial", devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazları tablosu getirilirken hata oluştu");
                return Json(new { success = false, message = "Tablo yüklenirken bir hata oluştu." });
            }
        }

       
    
        [HttpDelete("{id}")]
        [Route("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var device = await _context.TelemetryDevices.FindAsync(id);
                if (device == null)
                {
                    return Json(new { success = false, message = "Cihaz bulunamadı." });
                }

                _context.TelemetryDevices.Remove(device);
                await _context.SaveChangesAsync();

                // Aktivite kaydı
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(
                    userId,
                    "TelemetryDeviceDeleted",
                    $"Telemetri cihazı silindi: {device.SerialNumber}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString()
                );

                return Json(new { success = true, message = "Telemetri cihazı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazı silinirken hata oluştu");
                return Json(new { success = false, message = "Silme işlemi sırasında bir hata oluştu." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var device = await _context.TelemetryDevices
                    .Include(d => d.Vehicle)
                    .Include(d => d.Organization)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (device == null)
                    return NotFound("Telemetri cihazı bulunamadı.");

                return Ok(new
                {
                    device.Id,
                    device.SerialNumber,
                    device.SimCardNumber,
                    device.VehicleId,
                    device.OrganizationId,
                    device.FirmwareVersion,
                    device.InstallationDate,
                    device.Notes,
                    device.AktifMi,
                    device.IsOnline,
                    device.LastKnownLocation,
                    device.LastCommunication,
                    VehiclePlate = device.Vehicle?.Plaka,
                    OrganizationName = device.Organization?.KurumAdi
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetri cihazı getirilirken hata oluştu");
                return StatusCode(500, new { message = "Bir hata oluştu.", error = ex.Message });
            }
        }

    }

    public class TelemetryDeviceCreateDto
    {
        [Required(ErrorMessage = "Seri numarası zorunludur.")]
        [StringLength(50, ErrorMessage = "Seri numarası en fazla 50 karakter olabilir.")]
        public string SerialNumber { get; set; }

        [Required(ErrorMessage = "SIM kart numarası zorunludur.")]
        [StringLength(20, ErrorMessage = "SIM kart numarası en fazla 20 karakter olabilir.")]
        public string SimCardNumber { get; set; }

        [Required(ErrorMessage = "Araç seçimi zorunludur.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Organizasyon seçimi zorunludur.")]
        public int OrganizationId { get; set; }

        [StringLength(50)]
        public string? FirmwareVersion { get; set; }

        [Required(ErrorMessage = "Kurulum tarihi zorunludur.")]
        public DateTime InstallationDate { get; set; }

        public string? Notes { get; set; }

        public bool AktifMi { get; set; } = true;
    }

    public class TelemetryDeviceUpdateDto : TelemetryDeviceCreateDto
    {
        public int Id { get; set; }
        public bool AktifMi { get; set; }
    }
} 