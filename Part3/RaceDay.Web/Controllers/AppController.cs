using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    /// <summary>Shared behaviour for every controller: access to the API and the success/error banners.</summary>
    public abstract class AppController : Controller
    {
        protected readonly ApiService Api;

        protected AppController(ApiService api)
        {
            Api = api;
        }

        protected void Success(string message) => TempData["Success"] = message;

        protected void Failure(string message) => TempData["Error"] = message;

        /// <summary>
        /// If the API says our session is no longer valid (for example it timed out), sign the user out
        /// here too and send them to the login page.
        /// </summary>
        protected bool SessionExpired(ApiResponse response, out IActionResult redirect)
        {
            if (response.IsUnauthorized)
            {
                HttpContext.Session.Clear();
                Failure("Your session has expired. Please log in again.");
                redirect = RedirectToAction("Login", "Account");
                return true;
            }

            redirect = null!;
            return false;
        }
    }
}
