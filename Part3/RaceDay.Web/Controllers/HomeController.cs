using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>The public home page: upcoming events with filters.</summary>
    public class HomeController : AppController
    {
        public HomeController(ApiService api) : base(api) { }

        public async Task<IActionResult> Index(string? type, string? search, bool upcomingOnly = true)
        {
            // Organisers have their own home: the dashboard.
            if (HttpContext.Session.GetRole() == "Organiser")
                return RedirectToAction("Dashboard", "Organiser");

            var url = "api/events?upcomingOnly=" + (upcomingOnly ? "true" : "false");
            if (!string.IsNullOrWhiteSpace(type)) url += "&type=" + Uri.EscapeDataString(type);
            if (!string.IsNullOrWhiteSpace(search)) url += "&search=" + Uri.EscapeDataString(search);

            var response = await Api.GetAsync(url);

            var model = new HomeViewModel { Type = type, Search = search, UpcomingOnly = upcomingOnly };
            if (response.IsSuccess)
                model.Events = response.List<EventDto>();
            else
                Failure(response.Message);

            return View(model);
        }

        public IActionResult Error()
        {
            ViewBag.Heading = "Something went wrong";
            ViewBag.Detail = "Sorry, we could not complete that. Please try again.";
            return View();
        }

        public IActionResult StatusCodePage(int code)
        {
            ViewBag.Heading = code == 404 ? "Page not found" : "Something went wrong";
            ViewBag.Detail = code == 404
                ? "We could not find the page you were looking for."
                : "Sorry, we could not complete that. Please try again.";
            return View("Error");
        }
    }
}
