namespace dershane.Models
{
    public class UClass1
    {
        public int Id { get; set; }
        public string UClass { get; set; }   // Sınıf adı
        public string Student { get; set; }  // Öğrenci kullanıcı adı / id
        public bool IsTeacher { get; set; }  // true -> teacher, false -> student
    }
}