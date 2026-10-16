using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Age or distance categories within an event. Anyone can view them; only the owning Organiser can change them.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class CategoriesController : RaceDayControllerBase
    {
        public CategoriesController(RaceDayDbContext db) : base(db) { }

        private static object ToResponse(Category c) => new
        {
            c.CategoryID,
            c.EventID,
            c.Name,
            c.MinAge,
            c.MaxAge,
            c.DistanceKm
        };

        private static string? Validate(CategoryCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 50)
                return "Name is required and must be 50 characters or fewer.";
            if (dto.MinAge is < 0 || dto.MaxAge is < 0)
                return "Ages cannot be negative.";
            if (dto.MinAge is not null && dto.MaxAge is not null && dto.MinAge > dto.MaxAge)
                return "MinAge cannot be greater than MaxAge.";
            if (dto.DistanceKm is <= 0 or >= 1000)
                return "DistanceKm must be greater than 0 and less than 1000.";
            return null;
        }

        /// <summary>Lists the categories of an event.</summary>
        /// <remarks>Open to everyone, logged in or not.</remarks>
        /// <param name="eventId">The event id.</param>
        /// <response code="200">An array of categories.</response>
        /// <response code="404">No event with that id.</response>
        [HttpGet("api/events/{eventId}/categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            var eventExists = await Db.Events.AnyAsync(e => e.EventID == eventId);
            if (!eventExists) return NotFound(new { message = "Event not found." });

            var categories = await Db.Categories
                .AsNoTracking()
                .Where(c => c.EventID == eventId)
                .Select(c => new { c.CategoryID, c.EventID, c.Name, c.MinAge, c.MaxAge, c.DistanceKm })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>Adds a category to an event. Organiser only, and only their own events.</summary>
        /// <param name="eventId">The event id.</param>
        /// <param name="dto">Category name, optional age range and optional distance.</param>
        /// <response code="201">Category created. Returns the category.</response>
        /// <response code="400">A field is missing or invalid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No event with that id.</response>
        [HttpPost("api/events/{eventId}/categories")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(int eventId, CategoryCreateDto dto)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var ev = await Db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev is null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserID != organiser.OrganiserID) return Forbidden("You can only add categories to your own events.");

            var error = Validate(dto);
            if (error is not null) return BadRequest(new { message = error });

            var category = new Category
            {
                EventID = eventId,
                Name = dto.Name.Trim(),
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                DistanceKm = dto.DistanceKm
            };

            Db.Categories.Add(category);
            await Db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetForEvent), new { eventId }, ToResponse(category));
        }

        /// <summary>Updates a category. Organiser only, and only on their own events.</summary>
        /// <param name="id">The category id.</param>
        /// <param name="dto">The new category details. All fields are replaced.</param>
        /// <response code="200">Category updated. Returns the category.</response>
        /// <response code="400">A field is missing or invalid.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No category with that id.</response>
        [HttpPut("api/categories/{id}")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, CategoryCreateDto dto)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var category = await Db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category is null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserID != organiser.OrganiserID) return Forbidden("You can only change categories on your own events.");

            var error = Validate(dto);
            if (error is not null) return BadRequest(new { message = error });

            category.Name = dto.Name.Trim();
            category.MinAge = dto.MinAge;
            category.MaxAge = dto.MaxAge;
            category.DistanceKm = dto.DistanceKm;

            await Db.SaveChangesAsync();
            return Ok(ToResponse(category));
        }

        /// <summary>Deletes a category. Organiser only, and only on their own events.</summary>
        /// <remarks>A category that Participants have already enrolled under cannot be deleted.</remarks>
        /// <param name="id">The category id.</param>
        /// <response code="204">Category deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No category with that id.</response>
        /// <response code="409">Participants are enrolled under this category.</response>
        [HttpDelete("api/categories/{id}")]
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

            var category = await Db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category is null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserID != organiser.OrganiserID) return Forbidden("You can only delete categories on your own events.");

            var inUse = await Db.Enrolments.AnyAsync(en => en.CategoryID == id);
            if (inUse)
                return Conflict(new { message = "Participants are enrolled under this category, so it cannot be deleted." });

            Db.Categories.Remove(category);
            await Db.SaveChangesAsync();
            return NoContent();
        }
    }
}
