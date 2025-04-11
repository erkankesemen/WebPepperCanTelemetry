using System.ComponentModel.DataAnnotations;

namespace WebPepperCan.Models
{
    public class OrganizationCreateDto
    {
        [Required(ErrorMessage = "Kurum adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kurum adı en fazla 100 karakter olabilir.")]
        public string KurumAdi { get; set; }

        [StringLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
        public string? Adres { get; set; }

        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        public string? Tel { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string? Email { get; set; }

        [StringLength(100, ErrorMessage = "İlgili kişi adı en fazla 100 karakter olabilir.")]
        public string? IlgiliKisi { get; set; }
 
        public bool AktifMi { get; set; }

    }

    public class OrganizationUpdateDto : OrganizationCreateDto
    {
        public int Id { get; set; }
        public bool AktifMi { get; set; }
    }
} 