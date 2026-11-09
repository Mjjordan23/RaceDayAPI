using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Shared helpers for controllers that sit behind the session-based role check.
    /// </summary>
    [ApiController]
    public abstract class RaceDayControllerBase : ControllerBase
    {
        protected readonly RaceDayDbContext Db;

        protected RaceDayControllerBase(RaceDayDbContext db)
        {
            Db = db;
        }

        /// <summary>
        /// Returns a plain 403 response. Controllers use this instead of Forbid(), because
        /// Forbid() needs an authentication scheme to be configured and throws without one.
        /// </summary>
        protected ObjectResult Forbidden(string message = "You do not have permission to do this.")
            => StatusCode(StatusCodes.Status403Forbidden, new { message });

        /// <summary>The Organiser profile belonging to the logged-in user, or null.</summary>
        protected async Task<Organiser?> CurrentOrganiserAsync()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId is null) return null;
            return await Db.Organisers.FirstOrDefaultAsync(o => o.UserID == userId.Value);
        }

        /// <summary>The Participant profile belonging to the logged-in user, or null.</summary>
        protected async Task<Participant?> CurrentParticipantAsync()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId is null) return null;
            return await Db.Participants.FirstOrDefaultAsync(p => p.UserID == userId.Value);
        }
    }
}
