using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Registration, login and logout. Login starts a server-side session that remembers the user's id and role.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>Registers a new Organiser or Participant account.</summary>
        /// <remarks>
        /// Participants must send FirstName, LastName and DateOfBirth.
        /// Organisers must send OrganisationName.
        /// The password is hashed and never stored in plain text.
        /// </remarks>
        /// <param name="dto">The account details and chosen role.</param>
        /// <response code="201">Account created. Returns the user id, email and role.</response>
        /// <response code="400">A required field is missing or invalid.</response>
        /// <response code="409">That email is already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password are required." });

            var email = dto.Email.Trim().ToLowerInvariant();

            if (!email.Contains('@') || email.Length > 100)
                return BadRequest(new { message = "Please provide a valid email address of 100 characters or fewer." });

            if (dto.Password.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters." });

            if (dto.Role != "Organiser" && dto.Role != "Participant")
                return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

            // Validate the role-specific fields BEFORE saving anything, so a bad request
            // never leaves a half-created account behind.
            if (dto.Role == "Participant")
            {
                if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) || dto.DateOfBirth is null)
                    return BadRequest(new { message = "FirstName, LastName, and DateOfBirth are required for Participants." });
            }
            else if (string.IsNullOrWhiteSpace(dto.OrganisationName))
            {
                return BadRequest(new { message = "OrganisationName is required for Organisers." });
            }

            var exists = await _db.Users.AnyAsync(u => u.Email == email);
            if (exists)
                return Conflict(new { message = "Email is already registered." });

            var user = new User { Email = email, Role = dto.Role };
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            // The profile is attached to the user so both rows are saved together in one transaction.
            if (dto.Role == "Participant")
            {
                user.Participant = new Participant
                {
                    FirstName = dto.FirstName!.Trim(),
                    LastName = dto.LastName!.Trim(),
                    DateOfBirth = dto.DateOfBirth!.Value,
                    PhoneNumber = dto.PhoneNumber
                };
            }
            else
            {
                user.Organiser = new Organiser
                {
                    OrganisationName = dto.OrganisationName!.Trim(),
                    ContactNumber = dto.ContactNumber
                };
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.UserID }, new { user.UserID, user.Email, user.Role });
        }

        /// <summary>Logs in and starts a session.</summary>
        /// <remarks>
        /// On success the server stores the user's id and role in the session and sends back a session cookie.
        /// Every later request in the same browser or Swagger tab is treated as this user.
        /// </remarks>
        /// <param name="dto">Email and password.</param>
        /// <response code="200">Logged in. Returns the user id, email and role.</response>
        /// <response code="401">Email or password is wrong.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
                return Unauthorized(new { message = "Invalid email or password." });

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password ?? string.Empty);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Invalid email or password." });

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new { user.UserID, user.Email, user.Role });
        }

        /// <summary>Logs out and clears the session.</summary>
        /// <response code="200">Session cleared.</response>
        /// <response code="401">Not logged in.</response>
        [HttpPost("logout")]
        [RequireRole]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out." });
        }
    }
}
