using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>Create, edit, list and delete the Organiser's events, including the banner upload.</summary>
    [RequireLogin("Organiser")]
    public class OrganiserEventsController : AppController
    {
        public OrganiserEventsController(ApiService api) : base(api) { }

        public async Task<IActionResult> Index()
        {
            var response = await Api.GetAsync("api/events/mine");
            if (SessionExpired(response, out var redirect)) return redirect;

            if (!response.IsSuccess)
            {
                Failure(response.Message);
                return View(new List<MyEventDto>());
            }
            return View(response.List<MyEventDto>().OrderBy(e => e.EventDate).ToList());
        }

        [HttpGet]
        public IActionResult Create() => View("Form", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(7) });

        [HttpPost]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            var response = await Api.PostAsync("api/events", ToBody(model));
            if (SessionExpired(response, out var redirect)) return redirect;
            if (!response.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View("Form", model);
            }

            var created = response.Data<EventDto>();
            if (created is not null && model.Banner is { Length: > 0 })
            {
                var upload = await Api.PostFileAsync($"api/events/{created.EventID}/banner", model.Banner);
                if (!upload.IsSuccess)
                {
                    Success("The event was created.");
                    Failure("The banner could not be uploaded: " + upload.Message + " You can try again by editing the event.");
                    return RedirectToAction(nameof(Index));
                }
            }

            Success("Your event has been created. Next, add its categories.");
            return RedirectToAction("Index", "OrganiserCategories", new { eventId = created?.EventID });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var response = await Api.GetAsync($"api/events/{id}");
            if (SessionExpired(response, out var redirect)) return redirect;

            var ev = response.IsSuccess ? response.Data<EventDto>() : null;
            if (ev is null)
            {
                Failure(response.IsSuccess ? "That event could not be found." : response.Message);
                return RedirectToAction(nameof(Index));
            }

            return View("Form", new EventFormViewModel
            {
                EventID = ev.EventID,
                Name = ev.Name,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                DistanceKm = ev.DistanceKm,
                EventType = ev.EventType,
                BannerImageUrl = ev.BannerImageUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            model.EventID = id;
            if (!ModelState.IsValid) return View("Form", model);

            var response = await Api.PutAsync($"api/events/{id}", ToBody(model));
            if (SessionExpired(response, out var redirect)) return redirect;
            if (!response.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View("Form", model);
            }

            if (model.Banner is { Length: > 0 })
            {
                var upload = await Api.PostFileAsync($"api/events/{id}/banner", model.Banner);
                if (!upload.IsSuccess)
                {
                    Success("The event details were saved.");
                    Failure("The banner could not be uploaded: " + upload.Message);
                    return RedirectToAction(nameof(Index));
                }
            }

            Success("Your event has been updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await Api.DeleteAsync($"api/events/{id}");
            if (SessionExpired(response, out var redirect)) return redirect;

            if (response.IsSuccess) Success("The event has been deleted.");
            else Failure(response.Message);
            return RedirectToAction(nameof(Index));
        }

        private static object ToBody(EventFormViewModel m) => new
        {
            name = m.Name,
            description = m.Description,
            eventDate = m.EventDate,
            location = m.Location,
            distanceKm = m.DistanceKm,
            eventType = m.EventType
        };
    }
}
