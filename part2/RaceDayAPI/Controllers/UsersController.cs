using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Lets any logged-in user view and update their own profile.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : RaceDayControllerBase
    {
        public UsersController(RaceDayDbContext db) : base(db) { }

        /// <summary>Gets the logged-in user's own profile.</summary>
        /// <remarks>
        /// Works for both roles. Participants get name, date of birth, phone and profile picture fields;
        /// Organisers get organisation name and contact number. Fields that belong to the other role are null.
        /// </remarks>
        /// <response code="200">The profile of the logged-in user.</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet("me")]
        [RequireRole]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe()
        {
            var profile = await BuildProfileAsync(HttpContext.Session.GetUserId()!.Value);
            if (profile is null) return NotFound(new { message = "User not found." });
            return Ok(profile);
        }

        /// <summary>Updates the logged-in user's own profile.</summary>
        /// <remarks>
        /// Only the fields you send are changed. Participants can change FirstName, LastName,
        /// DateOfBirth and PhoneNumber. Organisers can change OrganisationName and ContactNumber.
        /// Fields that do not apply to your role are ignored. Email and role cannot be changed here.
        /// </remarks>
        /// <param name="dto">The profile fields to change.</param>
        /// <response code="200">The updated profile.</response>
        /// <response code="400">A value was empty, too long or invalid.</response>
        /// <response code="401">Not logged in.</response>
        [HttpPut("me")]
        [RequireRole]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var userId = HttpContext.Session.GetUserId()!.Value;
            var role = HttpContext.Session.GetRole();

            if (role == "Participant")
            {
                var participant = await CurrentParticipantAsync();
                if (participant is null) return NotFound(new { message = "Participant profile not found." });

                if (dto.FirstName is not null)
                {
                    if (string.IsNullOrWhiteSpace(dto.FirstName) || dto.FirstName.Trim().Length > 50)
                        return BadRequest(new { message = "FirstName must be 1 to 50 characters." });
                    participant.FirstName = dto.FirstName.Trim();
                }

                if (dto.LastName is not null)
                {
                    if (string.IsNullOrWhiteSpace(dto.LastName) || dto.LastName.Trim().Length > 50)
                        return BadRequest(new { message = "LastName must be 1 to 50 characters." });
                    participant.LastName = dto.LastName.Trim();
                }

                if (dto.DateOfBirth is not null)
                {
                    if (dto.DateOfBirth.Value > DateTime.UtcNow)
                        return BadRequest(new { message = "DateOfBirth cannot be in the future." });
                    participant.DateOfBirth = dto.DateOfBirth.Value;
                }

                if (dto.PhoneNumber is not null)
                {
                    if (dto.PhoneNumber.Trim().Length > 20)
                        return BadRequest(new { message = "PhoneNumber must be 20 characters or fewer." });
                    participant.PhoneNumber = dto.PhoneNumber.Trim();
                }
            }
            else
            {
                var organiser = await CurrentOrganiserAsync();
                if (organiser is null) return NotFound(new { message = "Organiser profile not found." });

                if (dto.OrganisationName is not null)
                {
                    if (string.IsNullOrWhiteSpace(dto.OrganisationName) || dto.OrganisationName.Trim().Length > 100)
                        return BadRequest(new { message = "OrganisationName must be 1 to 100 characters." });
                    organiser.OrganisationName = dto.OrganisationName.Trim();
                }

                if (dto.ContactNumber is not null)
                {
                    if (dto.ContactNumber.Trim().Length > 20)
                        return BadRequest(new { message = "ContactNumber must be 20 characters or fewer." });
                    organiser.ContactNumber = dto.ContactNumber.Trim();
                }
            }

            await Db.SaveChangesAsync();
            return Ok(await BuildProfileAsync(userId));
        }

        private async Task<UserProfileDto?> BuildProfileAsync(int userId)
        {
            var user = await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId);
            if (user is null) return null;

            var profile = new UserProfileDto
            {
                UserID = user.UserID,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            if (user.Role == "Participant")
            {
                var p = await Db.Participants.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == userId);
                if (p is not null)
                {
                    profile.ParticipantID = p.ParticipantID;
                    profile.FirstName = p.FirstName;
                    profile.LastName = p.LastName;
                    profile.DateOfBirth = p.DateOfBirth;
                    profile.PhoneNumber = p.PhoneNumber;
                    profile.ProfilePictureUrl = p.ProfilePictureUrl;
                }
            }
            else
            {
                var o = await Db.Organisers.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == userId);
                if (o is not null)
                {
                    profile.OrganiserID = o.OrganiserID;
                    profile.OrganisationName = o.OrganisationName;
                    profile.ContactNumber = o.ContactNumber;
                }
            }

            return profile;
        }
    }
}
