using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public EventsController(RaceDayDbContext db) => _db = db;

        // GET /api/events  (public - any user, logged in or not)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var events = await _db.Events
                .Select(e => new
                {
                    e.EventID,
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

        // GET /api/events/{id}  (public)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ev = await _db.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev is null) return NotFound(new { message = "Event not found." });

            return Ok(new
            {
                ev.EventID,
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

        // POST /api/events  (Organiser only)
        [HttpPost]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Create(EventCreateDto dto)
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (organiser is null) return Forbid();

            if (dto.EventType is not ("Run" or "Walk" or "Cycle"))
                return BadRequest(new { message = "EventType must be Run, Walk, or Cycle." });

            var ev = new Event
            {
                OrganiserID = organiser.OrganiserID,
                Name = dto.Name,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                DistanceKm = dto.DistanceKm,
                EventType = dto.EventType
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ev);
        }

        // PUT /api/events/{id}  (Organiser only, must own the event)
        [HttpPut("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Update(int id, EventCreateDto dto)
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (organiser is null) return Forbid();

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (ev is null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiser.OrganiserID)
                return Forbid(); // not the owner

            ev.Name = dto.Name;
            ev.Description = dto.Description;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;
            ev.DistanceKm = dto.DistanceKm;
            ev.EventType = dto.EventType;

            await _db.SaveChangesAsync();
            return Ok(ev);
        }

        // DELETE /api/events/{id}  (Organiser only, must own the event)
        [HttpDelete("{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (organiser is null) return Forbid();

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (ev is null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiser.OrganiserID)
                return Forbid();

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/events/{id}/enrolments  (Organiser only, must own the event)
        [HttpGet("{id}/enrolments")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> GetEnrolments(int id)
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (organiser is null) return Forbid();

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (ev is null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserID != organiser.OrganiserID) return Forbid();

            var enrolments = await _db.Enrolments
                .Where(en => en.EventID == id)
                .Include(en => en.Participant)
                .Include(en => en.Category)
                .Select(en => new
                {
                    en.EnrolmentID,
                    Participant = en.Participant.FirstName + " " + en.Participant.LastName,
                    Category = en.Category.Name,
                    en.Status,
                    en.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}
