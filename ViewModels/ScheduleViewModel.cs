
namespace dershane.ViewModels
{
    public class ScheduleViewModel
    {
        public List<ScheduleItem> ScheduleItems { get; set; } = new List<ScheduleItem>();
        public string CurrentClass { get; set; } = string.Empty;
        public string CurrentWeek { get; set; } = string.Empty;
    }

    public class ScheduleItem
    {
        public int Id { get; set; }
        public string Day { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
        public string Teacher { get; set; } = string.Empty;
        public string Classroom { get; set; } = string.Empty;
    }
}
