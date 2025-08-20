using System;
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class Exams
    {
        [Key]
        public int nid { get; set; }
        public string schoolnumber { get; set; }
        public string lesson { get; set; }
        public int points { get; set; }
    }

    public class ExamGroupVM
    {
        public string Lesson { get; set; }
        public List<ExamResultVM> ExamResults { get; set; }
    }
}
