using System.ComponentModel.DataAnnotations;

namespace dershane.ViewModels
{
    public class EditUserInfoVM
    {
        public string DershaneId { get; set; } = string.Empty;
        
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;
        
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;
        
        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }
        
        [Display(Name = "Telefon Numarası")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string? PhoneNumber { get; set; }
        
        [Display(Name = "Adres")]
        public string? Address { get; set; }
        
        // Öğrenciler için veli bilgileri
        [Display(Name = "Veli Adı")]
        public string? Parent { get; set; }
        
        [Display(Name = "Veli Telefon Numarası")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string? ParentPhoneNumber { get; set; }
        
        // Kullanıcının rolü (görüntüleme amaçlı)
        public string Role { get; set; } = string.Empty;
        
        // Kullanıcının sınıfı (görüntüleme amaçlı)
        public string? Class { get; set; }
    }
}