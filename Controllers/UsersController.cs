using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using WebPepperCan.Data;
using WebPepperCan.Models;
using WebPepperCan.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using WebPepperCan.Pages.Admin;
using System.Linq;

namespace WebPepperCan.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IActivityService _activityService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            ILogger<UsersController> logger,
            ApplicationDbContext context,
            IActivityService activityService)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _activityService = activityService;
        }

[HttpGet]
[Route("GetUser/{id}")]
public async Task<IActionResult> GetUser(string id)
{
    try
    {
        var user = await _userManager.Users
            .Include(u => u.Organization)  // Organization'ı include et
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı" });
        }

        var data = new {
            id = user.Id,
            name = user.Name,
            lastName = user.LastName,
            email = user.Email,
            organizationId = user.OrganizationId.ToString(), // string'e çevir
            organizationName = user.Organization?.KurumAdi,  // null check ekle
            aktifMi = user.AktifMi
        };

        return Json(new { success = true, data = data });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}

[HttpPost]
[Route("Create")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create([FromForm] CreateUserModel model)
{
    try
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Form verileri geçersiz" });
        }

        var user = new ApplicationUser
        {
            UserName = model.Email?.ToLower(),
            Email = model.Email?.ToLower(),
            Name = model.Name,
            LastName = model.LastName,
            OrganizationId = Convert.ToInt32(model.OrganizationId),
            AktifMi = model.AktifMi?.ToLower() == "true"
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return Json(new { success = false, message = result.Errors.First().Description });
        }

        return Json(new { success = true, message = "Kullanıcı başarıyla oluşturuldu" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}



[HttpPost]
[Route("Edit")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit([FromForm] UserEditModel model)
{
    try
    {
        _logger.LogInformation($"Gelen form verileri: {JsonSerializer.Serialize(model)}");

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            _logger.LogWarning($"Model doğrulama hataları: {errors}");
            return Json(new { success = false, message = "Form verileri geçersiz" });
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı" });
        }

        // Kullanıcı bilgilerini güncelle
        user.Name = model.Name;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.OrganizationId = Convert.ToInt32(model.OrganizationId);
        user.AktifMi = model.AktifMi?.ToLower() == "true";

        // Şifre değişikliği varsa uygula
        if (!string.IsNullOrEmpty(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!passwordResult.Succeeded)
            {
                return Json(new { success = false, message = "Şifre güncellenirken hata oluştu" });
            }
        }

        // Kullanıcıyı güncelle
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Json(new { success = false, message = "Kullanıcı güncellenirken hata oluştu" });
        }

        _logger.LogInformation($"Kullanıcı güncellendi. Yeni AktifMi değeri: {user.AktifMi}");
        return Json(new { success = true, message = "Kullanıcı başarıyla güncellendi" });
    }
    catch (Exception ex)
    {
        _logger.LogError($"Kullanıcı güncellenirken hata: {ex.Message}");
        return Json(new { success = false, message = ex.Message });
    }
}

[HttpGet]
[Route("GetTable")]
public async Task<IActionResult> GetTable()
{
    try
    {
        var users = await _userManager.Users
            .Include(u => u.Organization)
            .ToListAsync();

        return PartialView("_UsersTablePartial", users);
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Kullanıcı tablosu yüklenirken bir hata oluştu." });
    }
}

[HttpGet]
[Route("Edit/{id}")]
public async Task<IActionResult> Edit(string id)
{
    try
    {
        var user = await _userManager.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı" });
        }

        var data = new {
            id = user.Id,
            name = user.Name,
            lastName = user.LastName,
            email = user.Email,
            organizationId = user.OrganizationId.ToString(),
            organizationName = user.Organization?.KurumAdi,
            aktifMi = user.AktifMi
        };

        return Json(new { success = true, data = data });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}

[HttpDelete("{id}")]
[Route("Delete/{id}")]
public async Task<IActionResult> Delete(string id)
{
    try
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı" });
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            return Json(new { success = true, message = "Kullanıcı başarıyla silindi" });
        }
        else
        {
            return Json(new { success = false, message = "Kullanıcı silinirken bir hata oluştu" });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Kullanıcı silinirken hata oluştu");
        return Json(new { success = false, message = "Kullanıcı silinirken bir hata oluştu" });
    }
}

public class CreateUserModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Organizasyon seçimi zorunludur.")]
        public string OrganizationId { get; set; }

        public string AktifMi { get; set; } = "true";
    }

    public class UpdateUserModel
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string OrganizationId { get; set; }
        public string? Password { get; set; } // Opsiyonel
        public string AktifMi { get; set; }
    }

    public class UserEditModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string OrganizationId { get; set; }
        public string? Password { get; set; } // Opsiyonel
        public string AktifMi { get; set; }
    }
}
}
