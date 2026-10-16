using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Participants enter events under a category and view their own enrolments.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class EnrolmentsController : RaceDayControllerBase
    {
        public EnrolmentsController(RaceDayDbContext db) : base(db) { }

        /// <summary>Enters the logged-in Participant into an event under a chosen category. Participant only.</summary>
        /// <remarks>The Participant is taken from the session. The enrolment starts as Pending.</remarks>
        /// <param name="eventId">The event to enter.</param>
        /// <param name="dto">The id of a category that belongs to that event.</param>
        /// <response code="201">Enrolment recorded. Returns the enrolment.</response>
        /// <response code="400">The category does not belong to this event.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="404">No event with that id.</response>
        /// <response code="409">The Participant is already enrolled in this event.</response>
        [HttpPost("api/events/{eventId}/enrolments")]
        [RequireRole("Participant")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int eventId, EnrolmentCreateDto dto)
        {
            var participant = await CurrentParticipantAsync();
            if (participant is null) return Forbidden("Participant profile not found.");

            var ev = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev is null) return NotFound(new { message = "Event not found." });

            var category = await Db.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryId && c.EventID == eventId);
            if (category is null) return BadRequest(new { message = "Category does not belong to this event." });

            var alreadyEnrolled = await Db.Enrolments
                .AnyAsync(en => en.ParticipantID == participant.ParticipantID && en.EventID == eventId);
            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this event." });

            var enrolment = new Enrolment
            {
                ParticipantID = participant.ParticipantID,
                EventID = eventId,
                CategoryID = dto.CategoryId,
                Status = "Pending"
            };

            Db.Enrolments.Add(enrolment);
            await Db.SaveChangesAsync();

            return CreatedAtAction(nameof(MyEnrolments), null, new
            {
                enrolment.EnrolmentID,
                enrolment.ParticipantID,
                enrolment.EventID,
                enrolment.CategoryID,
                enrolment.Status,
                enrolment.EnrolledAt
            });
        }

        /// <summary>Lists every event the logged-in Participant has entered, with the category and status. Participant only.</summary>
        /// <response code="200">An array of the Participant's enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        [HttpGet("api/enrolments/me")]
        [RequireRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MyEnrolments()
        {
            var participant = await CurrentParticipantAsync();
            if (participant is null) return Forbidden("Participant profile not found.");

            var enrolments = await Db.Enrolments
                .AsNoTracking()
                .Where(en => en.ParticipantID == participant.ParticipantID)
                .OrderBy(en => en.Event.EventDate)
                .Select(en => new
                {
                    en.EnrolmentID,
                    en.EventID,
                    Event = en.Event.Name,
                    en.Event.EventDate,
                    en.CategoryID,
                    Category = en.Category.Name,
                    en.Status,
                    en.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}
