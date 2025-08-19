using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dershane.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Lesson name is required.")]
        [Display(Name = "Lesson")]
        public string Lesson { get; set; }

        [Required(ErrorMessage = "Teacher ID is required.")]
        [Display(Name = "Teacher")]
        public string TeacherId { get; set; }

        [Required(ErrorMessage = "Class is required.")]
        [Display(Name = "Class")]
        public string UClass { get; set; }

        [Required(ErrorMessage = "Day selection is required.")]
        [Range(0, 6, ErrorMessage = "Please select a valid day.")]
        [Display(Name = "Day")]
        public int Day { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public string StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public string EndTime { get; set; }
    }

    public class Lesson
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ScheduleViewModel
    {
        public int Id { get; set; }
        public string Lesson { get; set; }
        public string UClass { get; set; }
        public int Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string TeacherName { get; set; }
    }
}