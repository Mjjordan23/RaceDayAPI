using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>Manage the age/distance categories of one of the Organiser's events.</summary>
    [RequireLogin("Organiser")]
    public class OrganiserCategoriesController : AppController
    {
        public OrganiserCategoriesController(ApiService api) : base(api) { }

        public async Task<IActionResult> Index(int? eventId)
        {
            var page = await BuildPage(eventId);
            if (page.redirect is not null) return page.redirect;
            return View(page.model);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int eventId, CategoryFormViewModel newCategory)
        {
            // Only the category fields are validated here; the event comes from the route.
            if (!ModelState.IsValid || !AgesAreValid(newCategory))
            {
                var page = await BuildPage(eventId);
                if (page.redirect is not null) return page.redirect;
                page.model!.NewCategory = newCategory;
                return View("Index", page.model);
            }

            var response = await Api.PostAsync($"api/events/{eventId}/categories", ToBody(newCategory));
            if (SessionExpired(response, out var redirect)) return redirect;

            if (response.IsSuccess) Success("Category added.");
            else Failure(response.Message);
            return RedirectToAction(nameof(Index), new { eventId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int eventId, int id)
        {
            var response = await Api.DeleteAsync($"api/categories/{id}");
            if (SessionExpired(response, out var redirect)) return redirect;

            if (response.IsSuccess) Success("Category deleted.");
            else Failure(response.Message);
            return RedirectToAction(nameof(Index), new { eventId });
        }

        private bool AgesAreValid(CategoryFormViewModel c)
        {
            if (c.MinAge.HasValue && c.MaxAge.HasValue && c.MinAge > c.MaxAge)
            {
                ModelState.AddModelError("NewCategory.MaxAge", "Maximum age cannot be lower than minimum age.");
                return false;
            }
            return true;
        }

        private async Task<(CategoriesPageViewModel? model, IActionResult? redirect)> BuildPage(int? eventId)
        {
            var events = await Api.GetAsync("api/events/mine");
            if (SessionExpired(events, out var redirect)) return (null, redirect);

            var model = new CategoriesPageViewModel();
            if (!events.IsSuccess) Failure(events.Message);
            else model.Events = events.List<MyEventDto>().OrderBy(e => e.EventDate).ToList();

            model.SelectedEventId = eventId ?? model.Events.FirstOrDefault()?.EventID;
            if (model.SelectedEventId is int id)
            {
                var cats = await Api.GetAsync($"api/events/{id}/categories");
                if (SessionExpired(cats, out var r2)) return (null, r2);
                if (cats.IsSuccess) model.Categories = cats.List<CategoryDto>();
                else Failure(cats.Message);
            }
            return (model, null);
        }

        private static object ToBody(CategoryFormViewModel c) => new
        {
            name = c.Name,
            minAge = c.MinAge,
            maxAge = c.MaxAge,
            distanceKm = c.DistanceKm
        };
    }
}
