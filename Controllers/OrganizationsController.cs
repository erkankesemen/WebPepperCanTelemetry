using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using WebPepperCan.Data;
using System.Security.Claims;
using WebPepperCan.Services;
using WebPepperCan.Models;
using Microsoft.Extensions.Logging;
using WebPepperCan.Pages.Admin;
using System.Linq;
using YourNamespace.Extensions;

namespace WebPepperCan.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class OrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrganizationsController> _logger;
        private readonly IActivityService _activityService;

        public OrganizationsController(ApplicationDbContext context, ILogger<OrganizationsController> logger, IActivityService activityService)
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
                var organization = await _context.Organizations.FindAsync(id);
                if (organization == null)
                {
                    return Json(new { success = false, message = "Organizasyon bulunamadı" });
                }

                var data = new {
                    id = organization.Id,
                    kurumAdi = organization.KurumAdi,
                    tel = organization.Tel,
                    email = organization.Email,
                    ilgiliKisi = organization.IlgiliKisi,
                    adres = organization.Adres,
                    aktifMi = organization.AktifMi
                };

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organizasyon düzenleme formu getirilirken hata oluştu");
                return Json(new { success = false, message = "Organizasyon bilgileri alınamadı" });
            }
        }

        [HttpPost]
        [Route("Create", Name = "CreateOrganization")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] OrganizationCreateDto model)
        {
            try
            {
                var organization = new Organization
                {
                    KurumAdi = model.KurumAdi?.Trim(),
                    Adres = model.Adres?.Trim(),
                    Tel = model.Tel?.Trim(),
                    Email = model.Email?.Trim(),
                    IlgiliKisi = model.IlgiliKisi?.Trim(),
                    AktifMi = model.AktifMi,
                    KayitTarihi = DateTime.UtcNow
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                // Aktivite logu
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(userId, "OrganizationCreated", 
                    $"Yeni organizasyon eklendi: {organization.KurumAdi}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message = "Organizasyon başarıyla oluşturuldu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organizasyon oluşturulurken hata oluştu");
                return Json(new { success = false, message = "Organizasyon oluşturulurken bir hata oluştu" });
            }
        }

        [HttpPost]
        [Route("Edit", Name = "EditOrganization")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] OrganizationUpdateDto model)
        {
            try
            {
                var org = await _context.Organizations.FindAsync(model.Id);
                if (org == null)
                {
                    return Json(new { success = false, message = "Organizasyon bulunamadı" });
                }

                org.KurumAdi = model.KurumAdi?.Trim();
                org.Tel = model.Tel?.Trim();
                org.Email = model.Email?.Trim();
                org.IlgiliKisi = model.IlgiliKisi?.Trim();
                org.Adres = model.Adres?.Trim();
                org.AktifMi = model.AktifMi;

                await _context.SaveChangesAsync();

                // Aktivite logu
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityService.LogActivity(userId, "OrganizationUpdated", 
                    $"Organizasyon güncellendi: {org.KurumAdi}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message = "Organizasyon başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organizasyon güncellenirken hata oluştu");
                return Json(new { success = false, message = "Organizasyon güncellenirken bir hata oluştu" });
            }
        }

        [HttpGet]
        [Route("GetTable", Name = "GetOrganizationsTable")]
        public async Task<IActionResult> GetTable()
        {
            try
            {
                var organizations = await _context.Organizations
                    .Include(o => o.Users)
                    .Include(o => o.Vehicles)
                    .OrderByDescending(o => o.KayitTarihi)
                    .ToListAsync();

                return PartialView("_OrganizationsTablePartial", organizations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organizasyon tablosu getirilirken hata oluştu");
                return Json(new { success = false, message = "Tablo yüklenirken bir hata oluştu." });
            }
        }

        [HttpDelete("{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var organization = await _context.Organizations.FindAsync(id);
                if (organization == null)
                {
                    return Json(new { success = false, message = "Kurum bulunamadı" });
                }

                // İlişkili kayıtları kontrol et
                var hasUsers = await _context.Users.AnyAsync(u => u.OrganizationId == id);
                var hasVehicles = await _context.Vehicles.AnyAsync(v => v.OrganizationId == id);
                
                if (hasUsers || hasVehicles)
                {
                    return Json(new { success = false, message = "Bu organizasyona bağlı kullanıcılar veya araçlar var. Önce bunları silmelisiniz." });
                }

                _context.Organizations.Remove(organization);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Kurum başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organizasyon silinirken hata oluştu: {Message}", ex.Message);
                return Json(new { success = false, message = "Organizasyon silinirken bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        [Route("GetOrganization/{id:int}", Name = "GetOrganization")]
        public async Task<IActionResult> GetOrganization(int id)
        {
            try
            {
                var org = await _context.Organizations.FindAsync(id);
                if (org == null)
                {
                    return Json(new { success = false, message = "Organizasyon bulunamadı" });
                }

                var data = new {
                    id = org.Id,
                    kurumAdi = org.KurumAdi,
                    tel = org.Tel,
                    email = org.Email,
                    ilgiliKisi = org.IlgiliKisi,
                    adres = org.Adres,
                    aktifMi = org.AktifMi
                };

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organizasyon bilgileri getirilirken hata oluştu");
                return Json(new { success = false, message = "Organizasyon bilgileri alınamadı" });
            }
        }

        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var organizations = await _context.Organizations
                    .Where(o => o.AktifMi)
                    .OrderBy(o => o.KurumAdi)
                    .Select(o => new { id = o.Id, kurumAdi = o.KurumAdi })
                    .ToListAsync();

                return Json(new { success = true, data = organizations });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
