using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>Pages only a Participant can see: their enrolments and their race history.</summary>
    [RequireLogin("Participant")]
    public class ParticipantController : AppController
    {
        public ParticipantController(ApiService api) : base(api) { }

        public async Task<IActionResult> MyEvents()
        {
            var response = await Api.GetAsync("api/enrolments/me");
            if (SessionExpired(response, out var redirect)) return redirect;

            if (!response.IsSuccess)
            {
                Failure(response.Message);
                return View(new List<MyEnrolmentDto>());
            }

            return View(response.List<MyEnrolmentDto>().OrderBy(e => e.EventDate).ToList());
        }

        public async Task<IActionResult> MyResults()
        {
            var response = await Api.GetAsync("api/results/me");
            if (SessionExpired(response, out var redirect)) return redirect;

            if (!response.IsSuccess)
            {
                Failure(response.Message);
                return View(new List<MyResultDto>());
            }

            return View(response.List<MyResultDto>());
        }
    }
}
