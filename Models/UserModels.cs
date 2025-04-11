using System.ComponentModel.DataAnnotations;

namespace WebPepperCan.Models
{
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
        public int OrganizationId { get; set; }

        public bool AktifMi { get; set; } = true;
    }

    public class UpdateUserModel
    {
        [Required(ErrorMessage = "Ad alanı zorunludur")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "E-posta alanı zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Organizasyon seçimi zorunludur")]
        public int OrganizationId { get; set; }

        public string? Password { get; set; }

        public bool AktifMi { get; set; }
    }
}