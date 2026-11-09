using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// The Organiser's own events with enrolment totals, for the dashboard.
    /// </summary>
    [ApiController]
    [Route("api/events/mine")]
    [Produces("application/json")]
    public class OrganiserEventsController : RaceDayControllerBase
    {
        public OrganiserEventsController(RaceDayDbContext db) : base(db) { }

        /// <summary>Lists the logged-in Organiser's events, each with its number of categories and enrolments.</summary>
        /// <response code="200">An array of the Organiser's events, soonest first.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpGet]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMine()
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var events = await Db.Events.AsNoTracking()
                .Where(e => e.OrganiserID == organiser.OrganiserID)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.BannerImageUrl,
                    CategoryCount = e.Categories.Count,
                    EnrolmentCount = e.Enrolments.Count
                })
                .ToListAsync();

            return Ok(events);
        }
    }
}
