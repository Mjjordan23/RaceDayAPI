using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Organisers capture finish times and positions after an event; Participants view their own results.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class ResultsController : RaceDayControllerBase
    {
        public ResultsController(RaceDayDbContext db) : base(db) { }

        /// <summary>Captures the finish time and position for an enrolment. Organiser only, and only for their own events.</summary>
        /// <remarks>Also marks the enrolment as Confirmed. Each enrolment can have one result.</remarks>
        /// <param name="id">The enrolment id.</param>
        /// <param name="dto">Finish time (hh:mm:ss) and finishing position.</param>
        /// <response code="201">Result recorded. Returns the result.</response>
        /// <response code="400">The finish time or position is not valid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No enrolment with that id.</response>
        /// <response code="409">A result has already been captured for this enrolment.</response>
        [HttpPost("api/enrolments/{id}/results")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Capture(int id, ResultCreateDto dto)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var enrolment = await Db.Enrolments
                .Include(en => en.Event)
                .FirstOrDefaultAsync(en => en.EnrolmentID == id);

            if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
            if (enrolment.Event.OrganiserID != organiser.OrganiserID)
                return Forbidden("You can only capture results for your own events.");

            if (dto.FinishTime <= TimeSpan.Zero || dto.FinishTime >= TimeSpan.FromDays(1))
                return BadRequest(new { message = "FinishTime must be greater than 00:00:00 and less than 24 hours." });
            if (dto.FinishPosition < 1)
                return BadRequest(new { message = "FinishPosition must be 1 or higher." });

            var existing = await Db.Results.AnyAsync(r => r.EnrolmentID == id);
            if (existing)
                return Conflict(new { message = "A result has already been captured for this enrolment." });

            var result = new Result
            {
                EnrolmentID = id,
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition
            };

            Db.Results.Add(result);
            enrolment.Status = "Confirmed";
            await Db.SaveChangesAsync();

            return CreatedAtAction(nameof(MyResults), null, new
            {
                result.ResultID,
                result.EnrolmentID,
                result.FinishTime,
                result.FinishPosition,
                result.RecordedAt
            });
        }

        /// <summary>Lists the logged-in Participant's race history across all events with results. Participant only.</summary>
        /// <remarks>Each item includes the event, date, category, finish time, finishing position and the total number of finishers.</remarks>
        /// <response code="200">An array of the Participant's results.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpGet("api/results/me")]
        [RequireRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MyResults()
        {
            var participant = await CurrentParticipantAsync();
            if (participant is null) return Forbidden("Participant profile not found.");

            var results = await Db.Results
                .AsNoTracking()
                .Where(r => r.Enrolment.ParticipantID == participant.ParticipantID)
                .OrderByDescending(r => r.Enrolment.Event.EventDate)
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    r.Enrolment.EventID,
                    Event = r.Enrolment.Event.Name,
                    r.Enrolment.Event.EventDate,
                    Category = r.Enrolment.Category.Name,
                    r.FinishTime,
                    r.FinishPosition,
                    TotalFinishers = Db.Results.Count(x => x.Enrolment.EventID == r.Enrolment.EventID)
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>Lists all results for an event, ordered by finishing position.</summary>
        /// <remarks>Open to everyone, logged in or not.</remarks>
        /// <param name="eventId">The event id.</param>
        /// <response code="200">An array of results with participant name, category, time and position.</response>
        /// <response code="404">No event with that id.</response>
        [HttpGet("api/events/{eventId}/results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForEvent(int eventId)
        {
            var eventExists = await Db.Events.AnyAsync(e => e.EventID == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var results = await Db.Results
                .AsNoTracking()
                .Where(r => r.Enrolment.EventID == eventId)
                .OrderBy(r => r.FinishPosition)
                .Select(r => new
                {
                    r.ResultID,
                    Participant = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
                    Category = r.Enrolment.Category.Name,
                    r.FinishTime,
                    r.FinishPosition
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
