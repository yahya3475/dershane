using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class UserInformation
    {
        public int Id { get; set; }

        [Required]
        public string dershaneid { get; set; } = string.Empty;

        public string? email { get; set; }

        public string? phone_number { get; set; }

        public string? parent { get; set; }

        public string? parent_phone_number { get; set; }

        public string? address { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;
    }
}
