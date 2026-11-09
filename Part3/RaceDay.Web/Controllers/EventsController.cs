using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>The event detail page (anyone) and entering an event (Participants).</summary>
    public class EventsController : AppController
    {
        public EventsController(ApiService api) : base(api) { }

        public async Task<IActionResult> Details(int id)
        {
            var response = await Api.GetAsync($"api/events/{id}");
            var ev = response.IsSuccess ? response.Data<EventDetailDto>() : null;
            if (ev is null)
            {
                Failure(response.IsSuccess ? "That event could not be found." : response.Message);
                return RedirectToAction("Index", "Home");
            }

            var model = new EventDetailsViewModel { Event = ev };

            // A logged-in Participant sees whether they already entered this event.
            if (HttpContext.Session.GetRole() == "Participant")
            {
                var mine = await Api.GetAsync("api/enrolments/me");
                if (SessionExpired(mine, out var redirect)) return redirect;
                model.MyEnrolment = mine.List<MyEnrolmentDto>().FirstOrDefault(e => e.EventID == id);
            }

            return View(model);
        }

        [RequireLogin("Participant")]
        [HttpPost]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            if (categoryId <= 0)
            {
                Failure("Please choose a category.");
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            var response = await Api.PostAsync($"api/events/{eventId}/enrolments", new { categoryId });
            if (SessionExpired(response, out var redirect)) return redirect;

            if (!response.IsSuccess)
            {
                Failure(response.Message);
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            Success("You are entered! Your enrolment is pending until the organiser confirms it.");
            return RedirectToAction("MyEvents", "Participant");
        }
    }
}
