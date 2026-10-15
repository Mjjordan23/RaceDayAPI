using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public CategoriesController(RaceDayDbContext db) => _db = db;

        private async Task<Organiser?> GetOrganiserForCurrentUser()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId is null) return null;
            return await _db.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
        }

        // GET /api/events/{eventId}/categories  (public)
        [HttpGet("api/events/{eventId}/categories")]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var eventExists = await _db.Events.AnyAsync(e => e.EventID == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var categories = await _db.Categories
                .Where(c => c.EventID == eventId)
                .Select(c => new { c.CategoryID, c.Name, c.MinAge, c.MaxAge, c.DistanceKm })
                .ToListAsync();

            return Ok(categories);
        }

        // POST /api/events/{eventId}/categories  (Organiser, must own the event)
        [HttpPost("api/events/{eventId}/categories")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Create(int eventId, CategoryCreateDto dto)
        {
            var organiser = await GetOrganiserForCurrentUser();
            if (organiser is null) return Forbid();

            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev is null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserID != organiser.OrganiserID) return Forbid();

            var category = new Category
            {
                EventID = eventId,
                Name = dto.Name,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                DistanceKm = dto.DistanceKm
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetForEvent), new { eventId }, category);
        }

        // PUT /api/categories/{id}  (Organiser, must own the parent event)
        [HttpPut("api/categories/{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Update(int id, CategoryCreateDto dto)
        {
            var organiser = await GetOrganiserForCurrentUser();
            if (organiser is null) return Forbid();

            var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category is null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserID != organiser.OrganiserID) return Forbid();

            category.Name = dto.Name;
            category.MinAge = dto.MinAge;
            category.MaxAge = dto.MaxAge;
            category.DistanceKm = dto.DistanceKm;

            await _db.SaveChangesAsync();
            return Ok(category);
        }

        // DELETE /api/categories/{id}  (Organiser, must own the parent event)
        [HttpDelete("api/categories/{id}")]
        [RequireRole("Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var organiser = await GetOrganiserForCurrentUser();
            if (organiser is null) return Forbid();

            var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category is null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserID != organiser.OrganiserID) return Forbid();

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
