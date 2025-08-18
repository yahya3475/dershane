using System;
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ders adı zorunludur.")]
        [Display(Name = "Ders")]
        public string Lesson { get; set; }

        [Required(ErrorMessage = "Öğretmen ID zorunludur.")]
        [Display(Name = "Öğretmen")]
        public string TeacherId { get; set; }

        [Required(ErrorMessage = "Sınıf zorunludur.")]
        [Display(Name = "Sınıf")]
        public string UClass { get; set; }

        [Required(ErrorMessage = "Gün seçimi zorunludur.")]
        [Range(0, 6, ErrorMessage = "Geçerli bir gün seçiniz.")]
        [Display(Name = "Gün")]
        public int Day { get; set; }

        [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
        [Display(Name = "Başlangıç Saati")]
        [DataType(DataType.Time)]
        public string StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş saati zorunludur.")]
        [Display(Name = "Bitiş Saati")]
        [DataType(DataType.Time)]
        public string EndTime { get; set; }
    }
    
    public class Lesson
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}