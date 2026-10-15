using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthController(RaceDayDbContext db)
        {
            _db = db;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password are required." });

            if (dto.Role != "Organiser" && dto.Role != "Participant")
                return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                return Conflict(new { message = "Email is already registered." });

            var user = new User { Email = dto.Email, Role = dto.Role };
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            if (dto.Role == "Participant")
            {
                if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) || dto.DateOfBirth is null)
                    return BadRequest(new { message = "FirstName, LastName, and DateOfBirth are required for Participants." });

                _db.Participants.Add(new Participant
                {
                    UserID = user.UserID,
                    FirstName = dto.FirstName!,
                    LastName = dto.LastName!,
                    DateOfBirth = dto.DateOfBirth.Value,
                    PhoneNumber = dto.PhoneNumber
                });
            }
            else // Organiser
            {
                if (string.IsNullOrWhiteSpace(dto.OrganisationName))
                    return BadRequest(new { message = "OrganisationName is required for Organisers." });

                _db.Organisers.Add(new Organiser
                {
                    UserID = user.UserID,
                    OrganisationName = dto.OrganisationName!,
                    ContactNumber = dto.ContactNumber
                });
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.UserID }, new { user.UserID, user.Email, user.Role });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user is null)
                return Unauthorized(new { message = "Invalid email or password." });

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Invalid email or password." });

            HttpContext.Session.SetInt32("UserId", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new { user.UserID, user.Email, user.Role });
        }

        // POST /api/auth/logout
        [HttpPost("logout")]
        [RequireRole]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out." });
        }
    }
}
