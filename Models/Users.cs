namespace dershane.Models
{
    public class User
    {
        public int userid { get; set; }
        public string role { get; set; } = string.Empty;
        public string dershaneid { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string firstname { get; set; } = string.Empty;
        public string lastname { get; set; } = string.Empty;
        public bool firstlogin { get; set; } = true;

    }
}