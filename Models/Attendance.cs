using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public string Lesson { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public bool IsPresent { get; set; }

        public string? Note { get; set; }

        [Required]
        public string TeacherId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
