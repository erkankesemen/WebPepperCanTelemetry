using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;

namespace WebPepperCan.Pages
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SettingsModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public ApplicationUser UserInfo { get; set; }
        public DateTime? LastLoginDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Kullanıcı bilgilerini ve organizasyon detaylarını getir
            UserInfo = await _context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            // Son giriş tarihini al
            LastLoginDate = await _context.UserActivities
                .Where(ua => ua.UserId == user.Id && ua.ActivityType == "Login")
                .OrderByDescending(ua => ua.CreatedAt)
                .Select(ua => ua.CreatedAt)
                .FirstOrDefaultAsync();

            // Eğer son giriş tarihi yoksa şu anki zamanı kullan
            if (LastLoginDate == default(DateTime))
            {
                LastLoginDate = DateTime.Now;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (newPassword != confirmPassword)
            {
                ErrorMessage = "Yeni şifreler eşleşmiyor.";
                return RedirectToPage();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!changePasswordResult.Succeeded)
            {
                ErrorMessage = "Şifre değiştirilemedi. Mevcut şifrenizi kontrol edin.";
                return RedirectToPage();
            }

            SuccessMessage = "Şifreniz başarıyla değiştirildi.";
            return RedirectToPage();
        }
    }
} 