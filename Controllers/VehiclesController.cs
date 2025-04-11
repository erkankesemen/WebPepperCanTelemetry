using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebPepperCan.Data;
using System.Security.Claims;
using WebPepperCan.Services;
using WebPepperCan.Models.ViewModels;

namespace WebPepperCan.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VehiclesController> _logger;
        private readonly IActivityService _activityService;

        public VehiclesController(
            ApplicationDbContext context,
            ILogger<VehiclesController> logger,
            IActivityService activityService)
        {
            _context = context;
            _logger = logger;
            _activityService = activityService;
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] VehicleCreateDto model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Plaka) || string.IsNullOrEmpty(model.Sase))
                {
                    return Json(new { success = false, message = "Geçersiz veri." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // Organizasyon kontrolü
                var organization = await _context.Organizations.FindAsync(model.OrganizationId);
                if (organization == null)
                {
                    return Json(new { success = false, message = $"Organizasyon bulunamadı (ID: {model.OrganizationId})" });
                }

                // Plaka kontrolü
                var existingVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Plaka == model.Plaka.ToUpper());
                if (existingVehicle != null)
                {
                    return Json(new { success = false, message = $"Bu plaka numarası zaten kullanımda: {model.Plaka}" });
                }

                var vehicle = new Vehicle
                {
                    Plaka = model.Plaka.ToUpper().Trim(),
                    Sase = model.Sase.ToUpper().Trim(),
                    OrganizationId = model.OrganizationId,
                    Location = model.Location?.Trim(),
                    AktifMi = model.AktifMi,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Vehicles.AddAsync(vehicle);
                await _context.SaveChangesAsync();

                // Aktivite kaydı
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(userId, "VehicleCreated", 
                    $"Yeni araç eklendi: {vehicle.Plaka}", 
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message = "Araç başarıyla eklendi" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Araç eklenirken bir hata oluştu." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Organization)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (vehicle == null)
                {
                    return NotFound(new { message = $"Araç bulunamadı (ID: {id})" });
                }

                return Ok(new
                {
                    vehicle.Id,
                    vehicle.Plaka,
                    vehicle.Sase,
                    vehicle.Location,
                    vehicle.OrganizationId,
                    vehicle.AktifMi,
                    OrganizationName = vehicle.Organization?.KurumAdi
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç bilgileri alınırken bir hata oluştu." });
            }
        }

        [HttpGet]
        [Route("GetByOrganization/{organizationId}")]
        public async Task<IActionResult> GetByOrganization(int organizationId, [FromQuery] int? currentDeviceId = null)
        {
            try
            {
                var query = _context.Vehicles
                    .Include(v => v.TelemetryDevice)
                    .Where(v => v.OrganizationId == organizationId && v.AktifMi);

                // Araçları filtrele: Ya telemetri cihazı olmayanlar YA DA mevcut düzenlenen cihaza ait olan araç
                var vehicles = await query
                    .Where(v => v.TelemetryDevice == null || 
                           (currentDeviceId.HasValue && v.TelemetryDevice.Id == currentDeviceId))
                    .OrderBy(v => v.Plaka)
                    .Select(v => new { id = v.Id, plaka = v.Plaka })
                    .ToListAsync();

                return Json(vehicles);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Bir hata oluştu" });
            }
        }

        [HttpGet]
        [Route("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Organization)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (vehicle == null)
                {
                    return Json(new { success = false, message = "Araç bulunamadı" });
                }
                  var data = new {
                    id = vehicle.Id,
                    plaka = vehicle.Plaka,
                    sase = vehicle.Sase,
                    location = vehicle.Location,
                    organizationId = vehicle.OrganizationId,
                    aktifMi = vehicle.AktifMi,
                    organizationName = vehicle.Organization?.KurumAdi
                };

               
                
              

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Araç bilgileri alınamadı" });
            }
        }

        [HttpPost]
        [Route("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] VehicleUpdateDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var vehicle = await _context.Vehicles.FindAsync(model.Id);
                if (vehicle == null)
                {
                    return Json(new { success = false, message = "Araç bulunamadı." });
                }
               
          

          

               if (vehicle.OrganizationId != model.OrganizationId)
                {

                    var telemetriDevices = await _context.TelemetryDevices
                        .Where(td => td.VehicleId == vehicle.Id)
                        .ToListAsync();

                    if (telemetriDevices.Any())
                    {

                        // Telemetri cihazlarının OrganizationId'sini güncelle
                        foreach (var telemetriDevice in telemetriDevices)
                        {
                            telemetriDevice.OrganizationId = model.OrganizationId;
                        }

                        // Güncellenen telemetri devices'lerini veritabanına kaydet
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                    }
                }
                vehicle.Plaka = model.Plaka.ToUpper().Trim();
                vehicle.Sase = model.Sase.ToUpper().Trim();
                vehicle.OrganizationId = model.OrganizationId;
                vehicle.Location = model.Location?.Trim();
                vehicle.AktifMi = model.AktifMi;
                vehicle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Aktivite kaydı
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(userId, "VehicleUpdated", 
                    $"Araç güncellendi: {vehicle.Plaka}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message = "Araç başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Araç güncellenirken bir hata oluştu." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VehicleUpdateDto model)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogWarning("Model is null");
                    return BadRequest(new { message = "Geçersiz veri." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { message = "Geçersiz veri.", errors });
                }

                var vehicle = await _context.Vehicles
                    .Include(v => v.Organization)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (vehicle == null)
                {
                    return NotFound(new { message = $"Araç bulunamadı (ID: {id})" });
                }

              

                // Plaka kontrolü - başka araçta aynı plaka var mı?
                var existingVehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.Plaka == model.Plaka.ToUpper() && v.Id != model.Id);
                if (existingVehicle != null)
                {
                    return BadRequest(new { message = $"Bu plaka numarası zaten kullanımda: {model.Plaka}" });
                }
                  // Değişiklikleri uygula
                vehicle.Plaka = model.Plaka.ToUpper().Trim();
                vehicle.Sase = model.Sase.ToUpper().Trim();
                vehicle.OrganizationId = model.OrganizationId;
                vehicle.Location = model.Location?.Trim();
                vehicle.AktifMi = model.AktifMi;
                vehicle.UpdatedAt = DateTime.UtcNow;
                
                if (vehicle.OrganizationId != model.OrganizationId)
                {

                    var telemetriDevices = await _context.TelemetryDevices
                        .Where(td => td.VehicleId == vehicle.Id)
                        .ToListAsync();

                    if (telemetriDevices.Any())
                    {

                        // Telemetri cihazlarının OrganizationId'sini güncelle
                        foreach (var telemetriDevice in telemetriDevices)
                        {
                            telemetriDevice.OrganizationId = model.OrganizationId;
                        }

                        // Güncellenen telemetri devices'lerini veritabanına kaydet
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                    }
                }

                await _context.SaveChangesAsync();

                // Aktivite kaydı ekle
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(
                    userId,
                    "VehicleUpdated",
                    $"Araç güncellendi: {vehicle.Plaka} (Organizasyon: {vehicle.Organization?.KurumAdi})",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString()
                );

                return Ok(new { 
                    message = "Araç başarıyla güncellendi",
                    vehicle = new
                    {
                        vehicle.Id,
                        vehicle.Plaka,
                        vehicle.Sase,
                        vehicle.OrganizationId,
                        vehicle.Location,
                        vehicle.AktifMi,
                        OrganizationName = vehicle.Organization?.KurumAdi
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç güncellenirken bir hata oluştu." });
            }
        }

        [HttpDelete("{id}")]
        [Route("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.TelemetryDevice)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (vehicle == null)
                {
                    return Json(new { success = false, message = "Araç bulunamadı." });
                }

                if (vehicle.TelemetryDevice != null)
                {
                    return Json(new { 
                        success = false, 
                        message = "Bu araca tanımlı telemetri cihazı bulunmaktadır. Önce telemetri cihazı kaydını silmelisiniz." 
                    });
                }

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                // Aktivite kaydı
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(
                    userId,
                    "VehicleDeleted",
                    $"Araç silindi: {vehicle.Plaka}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString()
                );

                return Json(new { success = true, message = "Araç başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Silme işlemi sırasında bir hata oluştu." });
            }
        }
        
        [HttpGet]
        [Route("GetTable")]
        public async Task<IActionResult> GetTable()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Include(v => v.Organization)
                    .ToListAsync();

                return PartialView("_VehiclesTablePartial", vehicles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araç tablosu getirilirken hata oluştu");
                return Json(new { success = false, message = "Tablo yüklenirken bir hata oluştu." });
            }
        }

        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.AktifMi)
                    .OrderBy(v => v.Plaka)
                    .Select(v => new { id = v.Id, plaka = v.Plaka })
                    .ToListAsync();

                return Json(new { success = true, data = vehicles });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class VehicleCreateDto
        {
            [Required(ErrorMessage = "Plaka alanı zorunludur.")]
            [StringLength(20, ErrorMessage = "Plaka en fazla 20 karakter olabilir.")]
            public string? Plaka { get; set; }

            [Required(ErrorMessage = "Şase numarası zorunludur.")]
            [StringLength(50, ErrorMessage = "Şase numarası en fazla 50 karakter olabilir.")]
            public string? Sase { get; set; }

            [StringLength(200, ErrorMessage = "Konum en fazla 200 karakter olabilir.")]
            public string? Location { get; set; }

            [Required(ErrorMessage = "Organizasyon seçimi zorunludur.")]
            [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir organizasyon seçiniz.")]
            public int OrganizationId { get; set; }

            public bool AktifMi { get; set; }
        }

        public class VehicleUpdateDto
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Plaka alanı zorunludur.")]
            [StringLength(20, ErrorMessage = "Plaka en fazla 20 karakter olabilir.")]
            public string? Plaka { get; set; }

            [Required(ErrorMessage = "Şase numarası zorunludur.")]
            [StringLength(50, ErrorMessage = "Şase numarası en fazla 50 karakter olabilir.")]
            public string? Sase { get; set; }

            [Required(ErrorMessage = "Organizasyon seçimi zorunludur.")]
            public int OrganizationId { get; set; }

            [StringLength(200, ErrorMessage = "Konum en fazla 200 karakter olabilir.")]
            public string? Location { get; set; }

            public bool AktifMi { get; set; }
        }

        
    }
}