
public class ViewStudentsVM
{
    public string ClassName { get; set; } = string.Empty;
    public List<StudentDetailVM> Students { get; set; } = new List<StudentDetailVM>();
    public int TotalStudents { get; set; }
    public double ClassAverage { get; set; }
    
    public int ExcellentStudents => Students.Count(s => s.ExamAverage >= 85);
    public int GoodStudents => Students.Count(s => s.ExamAverage >= 70 && s.ExamAverage < 85);
    public int AverageStudents => Students.Count(s => s.ExamAverage >= 50 && s.ExamAverage < 70);
    public int NeedsImprovementStudents => Students.Count(s => s.ExamAverage < 50);
}
