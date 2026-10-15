using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public EnrolmentsController(RaceDayDbContext db) => _db = db;

        // POST /api/events/{eventId}/enrolments  (Participant only)
        [HttpPost("api/events/{eventId}/enrolments")]
        [RequireRole("Participant")]
        public async Task<IActionResult> Enrol(int eventId, EnrolmentCreateDto dto)
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var participant = await _db.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
            if (participant is null) return Forbid();

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev is null) return NotFound(new { message = "Event not found." });

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryId && c.EventID == eventId);
            if (category is null) return BadRequest(new { message = "Category does not belong to this event." });

            var alreadyEnrolled = await _db.Enrolments
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

            _db.Enrolments.Add(enrolment);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(MyEnrolments), null, enrolment);
        }

        // GET /api/enrolments/me  (Participant only)
        [HttpGet("api/enrolments/me")]
        [RequireRole("Participant")]
        public async Task<IActionResult> MyEnrolments()
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var participant = await _db.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
            if (participant is null) return Forbid();

            var enrolments = await _db.Enrolments
                .Where(en => en.ParticipantID == participant.ParticipantID)
                .Include(en => en.Event)
                .Include(en => en.Category)
                .Select(en => new
                {
                    en.EnrolmentID,
                    Event = en.Event.Name,
                    en.Event.EventDate,
                    Category = en.Category.Name,
                    en.Status,
                    en.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}
