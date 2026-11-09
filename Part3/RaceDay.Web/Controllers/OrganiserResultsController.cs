using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>Capture finish times and positions for the people who entered an event.</summary>
    [RequireLogin("Organiser")]
    public class OrganiserResultsController : AppController
    {
        public OrganiserResultsController(ApiService api) : base(api) { }

        public async Task<IActionResult> Index(int? eventId)
        {
            var events = await Api.GetAsync("api/events/mine");
            if (SessionExpired(events, out var redirect)) return redirect;

            var model = new ResultsPageViewModel();
            if (!events.IsSuccess) Failure(events.Message);
            else model.Events = events.List<MyEventDto>().OrderBy(e => e.EventDate).ToList();

            model.SelectedEventId = eventId ?? model.Events.FirstOrDefault()?.EventID;
            if (model.SelectedEventId is int id)
            {
                var enrolments = await Api.GetAsync($"api/events/{id}/enrolments");
                if (SessionExpired(enrolments, out var r2)) return r2;
                var results = await Api.GetAsync($"api/events/{id}/results");
                if (SessionExpired(results, out var r3)) return r3;

                if (!enrolments.IsSuccess) Failure(enrolments.Message);
                else if (!results.IsSuccess) Failure(results.Message);
                else
                {
                    var resultList = results.List<EventResultDto>();
                    model.Rows = enrolments.List<EventEnrolmentDto>()
                        .Select(en => new ResultRowViewModel
                        {
                            Enrolment = en,
                            Result = resultList.FirstOrDefault(r => r.EnrolmentID == en.EnrolmentID)
                        })
                        .OrderBy(r => r.Result?.FinishPosition ?? int.MaxValue)
                        .ThenBy(r => r.Enrolment.ParticipantName)
                        .ToList();
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Capture(int eventId, int enrolmentId, string? finishTime, int? finishPosition)
        {
            // Accept 1:05:30, 01:05:30 or 0:48:12 (hours:minutes:seconds).
            if (!TimeSpan.TryParseExact(finishTime?.Trim(), new[] { @"h\:mm\:ss", @"hh\:mm\:ss" },
                    CultureInfo.InvariantCulture, out var time) || time <= TimeSpan.Zero)
            {
                Failure("Please enter the finish time as hours:minutes:seconds, for example 1:05:30.");
                return RedirectToAction(nameof(Index), new { eventId });
            }
            if (finishPosition is null or < 1)
            {
                Failure("Please enter a finishing position of 1 or higher.");
                return RedirectToAction(nameof(Index), new { eventId });
            }

            var response = await Api.PostAsync($"api/enrolments/{enrolmentId}/results", new
            {
                finishTime = time.ToString("c", CultureInfo.InvariantCulture),
                finishPosition
            });
            if (SessionExpired(response, out var redirect)) return redirect;

            if (response.IsSuccess) Success("Result saved. The entry is now confirmed.");
            else Failure(response.Message);
            return RedirectToAction(nameof(Index), new { eventId });
        }
    }
}
