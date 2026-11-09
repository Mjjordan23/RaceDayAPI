using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>See who has entered one of the Organiser's events.</summary>
    [RequireLogin("Organiser")]
    public class OrganiserEnrolmentsController : AppController
    {
        public OrganiserEnrolmentsController(ApiService api) : base(api) { }

        public async Task<IActionResult> Index(int? eventId)
        {
            var events = await Api.GetAsync("api/events/mine");
            if (SessionExpired(events, out var redirect)) return redirect;

            var model = new EnrolmentsPageViewModel();
            if (!events.IsSuccess) Failure(events.Message);
            else model.Events = events.List<MyEventDto>().OrderBy(e => e.EventDate).ToList();

            model.SelectedEventId = eventId ?? model.Events.FirstOrDefault()?.EventID;
            if (model.SelectedEventId is int id)
            {
                var list = await Api.GetAsync($"api/events/{id}/enrolments");
                if (SessionExpired(list, out var r2)) return r2;
                if (list.IsSuccess) model.Enrolments = list.List<EventEnrolmentDto>();
                else Failure(list.Message);
            }
            return View(model);
        }
    }
}
