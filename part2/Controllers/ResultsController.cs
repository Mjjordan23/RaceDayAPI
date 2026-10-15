using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public ResultsController(RaceDayDbContext db) => _db = db;

        // POST /api/enrolments/{id}/results  (Organiser only, must own the event)
        [HttpPost("api/enrolments/{id}/results")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Capture(int id, ResultCreateDto dto)
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            if (organiser is null) return Forbid();

            var enrolment = await _db.Enrolments
                .Include(en => en.Event)
                .FirstOrDefaultAsync(en => en.EnrolmentID == id);

            if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
            if (enrolment.Event.OrganiserID != organiser.OrganiserID) return Forbid();

            var existing = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentID == id);
            if (existing is not null)
                return Conflict(new { message = "A result has already been captured for this enrolment." });

            var result = new Result
            {
                EnrolmentID = id,
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition
            };

            _db.Results.Add(result);
            enrolment.Status = "Confirmed";
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(MyResults), null, result);
        }

        // GET /api/results/me  (Participant only)
        [HttpGet("api/results/me")]
        [RequireRole("Participant")]
        public async Task<IActionResult> MyResults()
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var participant = await _db.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
            if (participant is null) return Forbid();

            var results = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(en => en.Event)
                .Include(r => r.Enrolment).ThenInclude(en => en.Category)
                .Where(r => r.Enrolment.ParticipantID == participant.ParticipantID)
                .Select(r => new
                {
                    Event = r.Enrolment.Event.Name,
                    r.Enrolment.Event.EventDate,
                    Category = r.Enrolment.Category.Name,
                    r.FinishTime,
                    r.FinishPosition
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET /api/events/{eventId}/results  (public)
        [HttpGet("api/events/{eventId}/results")]
        public async Task<IActionResult> ForEvent(int eventId)
        {
            var eventExists = await _db.Events.AnyAsync(e => e.EventID == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var results = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(en => en.Participant)
                .Include(r => r.Enrolment).ThenInclude(en => en.Category)
                .Where(r => r.Enrolment.EventID == eventId)
                .OrderBy(r => r.FinishPosition)
                .Select(r => new
                {
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
