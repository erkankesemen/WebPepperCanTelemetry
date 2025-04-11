using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebPepperCan.Data;

namespace WebPepperCan.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public class UserViewModel
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public bool EmailConfirmed { get; set; }
            public List<string> Roles { get; set; }
        }

        public List<UserViewModel> Users { get; set; }

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            Users = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                Users.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList()
                });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Admin kullanıcısının silinmesini engelle
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                StatusMessage = "Admin kullanıcısı silinemez!";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = "Kullanıcı başarıyla silindi.";
            }
            else
            {
                StatusMessage = "Kullanıcı silinirken bir hata oluştu.";
            }

            return RedirectToPage();
        }
    }
}
