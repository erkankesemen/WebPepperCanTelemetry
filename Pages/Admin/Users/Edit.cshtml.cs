using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebPepperCan.Data;
using Microsoft.EntityFrameworkCore;

namespace WebPepperCan.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public List<string> AvailableRoles { get; set; }
        public List<string> UserRoles { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            public string Id { get; set; }

            [Required(ErrorMessage = "Email adresi gereklidir")]
            [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
            public string Email { get; set; }

            [StringLength(100, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = user.Id,
                Email = user.Email
            };

            AvailableRoles = (await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync())!;

            UserRoles = (await _userManager.GetRolesAsync(user)).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string[] selectedRoles)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Email güncelleme
            if (user.Email != Input.Email)
            {
                user.Email = Input.Email;
                user.UserName = Input.Email;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }

            // Şifre güncelleme
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var passwordResult = await _userManager.RemovePasswordAsync(user);
                if (passwordResult.Succeeded)
                {
                    passwordResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return Page();
                    }
                }
            }

            // Rolleri güncelleme
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Admin kullanıcısından Admin rolünü kaldırmayı engelle
            if (user.Email == "admin@webpeppercan.com" && 
                !selectedRoles.Contains("Admin") && 
                currentRoles.Contains("Admin"))
            {
                StatusMessage = "Admin kullanıcısından Admin rolü kaldırılamaz!";
                return RedirectToPage();
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Roller güncellenirken hata oluştu.");
                return Page();
            }

            if (selectedRoles != null && selectedRoles.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Roller güncellenirken hata oluştu.");
                    return Page();
                }
            }

            StatusMessage = "Kullanıcı başarıyla güncellendi.";
            return RedirectToPage("./Index");
        }
    }
}
