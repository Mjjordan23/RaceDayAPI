using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Filters
{
    /// <summary>
    /// Protects a page. Not logged in: redirect to the login page. Logged in with the wrong role:
    /// redirect home with a message. Use [RequireLogin] for any user, or [RequireLogin("Organiser")]
    /// for one role. This mirrors the [RequireRole] filter in the API.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        private readonly string? _role;

        public RequireLoginAttribute(string? role = null)
        {
            _role = role;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (!session.IsLoggedIn())
            {
                SetMessage(context, "Please log in to continue.");
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (_role is not null && session.GetRole() != _role)
            {
                SetMessage(context, "You do not have access to that page.");
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }

        private static void SetMessage(ActionExecutingContext context, string message)
        {
            var factory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            factory.GetTempData(context.HttpContext)["Error"] = message;
        }
    }
}
