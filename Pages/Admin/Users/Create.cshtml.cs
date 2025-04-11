using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WebPepperCan.Data;
using System.Text.Json;

namespace WebPepperCan.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CreateModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email adresi gereklidir")]
            [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Organizasyon seçimi gereklidir")]
            public int OrganizationId { get; set; }

            [Required(ErrorMessage = "Şifre gereklidir")]
            [StringLength(100, ErrorMessage = "Şifre en az {2} karakter uzunluğunda olmalıdır", MinimumLength = 6)]
            public string Password { get; set; }

            [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
            [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet()
        {
            // API çağrıları için NotFound dönme
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return NotFound();
            }
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync([FromBody] CreateUserRequest request)
        {
            try 
            {
                // Debug için
                System.Diagnostics.Debug.WriteLine($"Request received: {JsonSerializer.Serialize(request)}");
                System.Diagnostics.Debug.WriteLine($"ModelState valid: {ModelState.IsValid}");

                if (request?.Input == null)
                {
                    System.Diagnostics.Debug.WriteLine("Request or Input is null");
                    return BadRequest(new { succeeded = false, errors = new[] { "Geçersiz veri" } });
                }

                // Input modelini güncelle
                Input = new InputModel
                {
                    Email = request.Input.Email,
                    Password = request.Input.Password,
                    ConfirmPassword = request.Input.ConfirmPassword,
                    OrganizationId = request.Input.OrganizationId
                };

                System.Diagnostics.Debug.WriteLine($"Input model: {JsonSerializer.Serialize(Input)}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"ModelState errors: {string.Join(", ", errors)}");
                    return new JsonResult(new { succeeded = false, errors = errors });
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    OrganizationId = Input.OrganizationId
                };

                var result = await _userManager.CreateAsync(user, Input.Password);
                System.Diagnostics.Debug.WriteLine($"User creation result: {JsonSerializer.Serialize(result)}");

                if (result.Succeeded)
                {
                    // Kullanıcı aktivitesini kaydet
                    var activity = new UserActivity
                    {
                        UserId = user.Id,
                        ActivityType = "Kullanıcı oluşturuldu",
                        Description = $"Email: {user.Email}",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers["User-Agent"].ToString()
                    };
                    _context.UserActivities.Add(activity);
                    await _context.SaveChangesAsync();

                    return new JsonResult(new { succeeded = true });
                }

                return new JsonResult(new { 
                    succeeded = false, 
                    errors = result.Errors.Select(e => e.Description).ToList() 
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
                return new JsonResult(new { succeeded = false, errors = new[] { ex.Message } });
            }
        }
    }

    public class CreateUserRequest
    {
        public class InputModel
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
            public int OrganizationId { get; set; }
        }

        public InputModel Input { get; set; }
    }
}
