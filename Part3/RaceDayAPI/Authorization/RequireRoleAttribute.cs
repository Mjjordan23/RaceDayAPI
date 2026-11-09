using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayAPI.Authorization
{
    /// <summary>
    /// Session-based role check. Use [RequireRole("Organiser")] or
    /// [RequireRole("Participant")] on a controller/action. Use [RequireRole]
    /// with no argument to just require any logged-in user.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireRoleAttribute : Attribute, IActionFilter
    {
        private readonly string? _role;

        public RequireRoleAttribute(string? role = null)
        {
            _role = role;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userId = session.GetInt32("UserId");
            var role = session.GetString("Role");

            if (userId is null || string.IsNullOrEmpty(role))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Not logged in." });
                return;
            }

            if (_role is not null && !string.Equals(role, _role, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new { message = $"This action requires the {_role} role." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    /// <summary>Small helper for controllers to read the current session user.</summary>
    public static class SessionExtensions
    {
        public static int? GetUserId(this ISession session) => session.GetInt32("UserId");
        public static string? GetRole(this ISession session) => session.GetString("Role");
    }
}
