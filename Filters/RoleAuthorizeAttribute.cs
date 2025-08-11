using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace dershane.Filters
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string _role;

        public RoleAuthorizeAttribute(string role)
        {
            _role = role;
        }
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userRole = context.HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(userRole) || userRole != _role)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }

            base.OnActionExecuting(context);
        }
    }
}