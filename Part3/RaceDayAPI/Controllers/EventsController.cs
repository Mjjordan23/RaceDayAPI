using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Browse events (anyone) and create, edit or delete them (Organisers, own events only).
    /// </summary>
    [ApiController]
    [Route("api/events")]
    [Produces("application/json")]
    public class EventsController : RaceDayControllerBase
    {
        private static readonly string[] ValidTypes = { "Run", "Walk", "Cycle" };

        public EventsController(RaceDayDbContext db) : base(db) { }

        private static object ToResponse(Event e) => new
        {
            e.EventID,
            e.OrganiserID,
            e.Name,
            e.Description,
            e.EventDate,
            e.Location,
            e.DistanceKm,
            e.EventType,
            e.BannerImageUrl
        };

        private static string? Validate(EventCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 100)
                return "Name is required and must be 100 characters or fewer.";
            if (string.IsNullOrWhiteSpace(dto.Description))
                return "Description is required.";
            if (string.IsNullOrWhiteSpace(dto.Location) || dto.Location.Trim().Length > 100)
                return "Location is required and must be 100 characters or fewer.";
            if (dto.DistanceKm <= 0 || dto.DistanceKm >= 1000)
                return "DistanceKm must be greater than 0 and less than 1000.";
            if (!ValidTypes.Contains(dto.EventType))
                return "EventType must be Run, Walk, or Cycle.";
            return null;
        }

        /// <summary>Lists events, with optional filters.</summary>
        /// <remarks>Open to everyone, logged in or not. Results are ordered by date, soonest first.</remarks>
        /// <param name="type">Optional. Only events of this type: Run, Walk or Cycle.</param>
        /// <param name="search">Optional. Only events whose name or location contains this text.</param>
        /// <param name="upcomingOnly">Optional. When true, hides events that have already taken place. Default false.</param>
        /// <response code="200">An array of events.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? search, [FromQuery] bool upcomingOnly = false)
        {
            var query = Db.Events.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(e => e.EventType == type);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Name.Contains(search) || e.Location.Contains(search));

            if (upcomingOnly)
                query = query.Where(e => e.EventDate >= DateTime.UtcNow);

            var events = await query
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.OrganiserID,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.BannerImageUrl
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>Gets one event, including its categories.</summary>
        /// <param name="id">The event id.</param>
        /// <response code="200">The event and its categories.</response>
        /// <response code="404">No event with that id.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var ev = await Db.Events.AsNoTracking()
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev is null) return NotFound(new { message = "Event not found." });

            return Ok(new
            {
                ev.EventID,
                ev.OrganiserID,
                ev.Name,
                ev.Description,
                ev.EventDate,
                ev.Location,
                ev.DistanceKm,
                ev.EventType,
                ev.BannerImageUrl,
                Categories = ev.Categories.Select(c => new { c.CategoryID, c.Name, c.MinAge, c.MaxAge, c.DistanceKm })
            });
        }

        /// <summary>Creates a new event. Organiser only.</summary>
        /// <remarks>The event is owned by the logged-in Organiser.</remarks>
        /// <param name="dto">Name, description, date, location, distance and type (Run, Walk or Cycle).</param>
        /// <response code="201">Event created. Returns the new event.</response>
        /// <response code="400">A field is missing or invalid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant.</response>
        [HttpPost]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create(EventCreateDto dto)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var error = Validate(dto);
            if (error is not null) return BadRequest(new { message = error });

            var ev = new Event
            {
                OrganiserID = organiser.OrganiserID,
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                EventDate = dto.EventDate,
                Location = dto.Location.Trim(),
                DistanceKm = dto.DistanceKm,
                EventType = dto.EventType
            };

            Db.Events.Add(ev);
            await Db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ToResponse(ev));
        }

        /// <summary>Updates an event. Organiser only, and only their own events.</summary>
        /// <param name="id">The event id.</param>
        /// <param name="dto">The new details. All fields are replaced.</param>
        /// <response code="200">Event updated. Returns the event.</response>
        /// <response code="400">A field is missing or invalid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No event with that id.</response>
        [HttpPut("{id}")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, EventCreateDto dto)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var ev = await Db.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (ev is null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserID != organiser.OrganiserID) return Forbidden("You can only change your own events.");

            var error = Validate(dto);
            if (error is not null) return BadRequest(new { message = error });

            ev.Name = dto.Name.Trim();
            ev.Description = dto.Description.Trim();
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location.Trim();
            ev.DistanceKm = dto.DistanceKm;
            ev.EventType = dto.EventType;

            await Db.SaveChangesAsync();
            return Ok(ToResponse(ev));
        }

        /// <summary>Deletes an event. Organiser only, and only their own events.</summary>
        /// <remarks>An event that already has enrolments cannot be deleted.</remarks>
        /// <param name="id">The event id.</param>
        /// <response code="204">Event deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No event with that id.</response>
        /// <response code="409">The event has enrolments, so it cannot be deleted.</response>
        [HttpDelete("{id}")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var ev = await Db.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (ev is null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserID != organiser.OrganiserID) return Forbidden("You can only delete your own events.");

            var hasEnrolments = await Db.Enrolments.AnyAsync(en => en.EventID == id);
            if (hasEnrolments)
                return Conflict(new { message = "This event already has enrolments and cannot be deleted." });

            Db.Events.Remove(ev);
            await Db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Lists everyone enrolled in an event. Organiser only, and only their own events.</summary>
        /// <param name="id">The event id.</param>
        /// <response code="200">An array of enrolments with participant name, category and status.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No event with that id.</response>
        [HttpGet("{id}/enrolments")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolments(int id)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var ev = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
            if (ev is null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserID != organiser.OrganiserID) return Forbidden("You can only view enrolments for your own events.");

            var enrolments = await Db.Enrolments
                .AsNoTracking()
                .Where(en => en.EventID == id)
                .Select(en => new
                {
                    en.EnrolmentID,
                    en.ParticipantID,
                    Participant = en.Participant.FirstName + " " + en.Participant.LastName,
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
