using Microsoft.AspNetCore.Mvc;
using dershane.Filters;

namespace dershane.Controllers
{
    [SessionAuthorizer]
    [RoleAuthorize("teacher")]
    public class TeacherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}