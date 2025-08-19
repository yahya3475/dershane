
namespace dershane.ViewModels
{
    public class SubmissionListVM
    {
        public int Id { get; set; }
        public string Answer { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int? Grade { get; set; }
        public string TeacherComment { get; set; }
        public bool IsGraded { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
    }
}
