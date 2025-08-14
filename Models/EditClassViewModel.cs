using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
public class EditClassViewModel
{
    public string ClassName { get; set; }
    public string NewClassName { get; set; }
    [Required(ErrorMessage = "Please select a teacher")]
    public string TeacherId { get; set; }
    public List<User>? Students { get; set; }
    public User? Teacher { get; set; }
}
}