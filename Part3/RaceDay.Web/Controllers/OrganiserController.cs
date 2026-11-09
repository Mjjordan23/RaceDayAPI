using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>The Organiser's home page: totals and a list of their events.</summary>
    [RequireLogin("Organiser")]
    public class OrganiserController : AppController
    {
        public OrganiserController(ApiService api) : base(api) { }

        public async Task<IActionResult> Dashboard()
        {
            var response = await Api.GetAsync("api/events/mine");
            if (SessionExpired(response, out var redirect)) return redirect;

            var model = new DashboardViewModel();
            if (response.IsSuccess) model.Events = response.List<MyEventDto>();
            else Failure(response.Message);
            return View(model);
        }
    }
}
