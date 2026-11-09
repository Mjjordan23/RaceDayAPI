using Azure;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Authorization;
using RaceDayAPI.Data;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers
{
    /// <summary>
    /// Image uploads. The web app sends files here, and only this API talks to Azure Blob Storage.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public class UploadsController : RaceDayControllerBase
    {
        private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly IBlobStorageService _blobs;

        public UploadsController(RaceDayDbContext db, IBlobStorageService blobs) : base(db)
        {
            _blobs = blobs;
        }

        /// <summary>Uploads a banner image for an event. Organiser only, and only their own events.</summary>
        /// <remarks>Send the image as multipart/form-data in a field called "file". JPG, PNG or WEBP, up to 5 MB.</remarks>
        /// <param name="id">The event id.</param>
        /// <param name="file">The banner image.</param>
        /// <response code="200">Banner saved. Returns the event id and the image URL.</response>
        /// <response code="400">No file, wrong file type, or the file is too big.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as a Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No event with that id.</response>
        /// <response code="503">Image storage is not available.</response>
        [HttpPost("events/{id}/banner")]
        [Consumes("multipart/form-data")]
        [RequireRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file)
        {
            var organiser = await CurrentOrganiserAsync();
            if (organiser is null) return Forbidden("Organiser profile not found.");

            var ev = await Db.Events.FindAsync(id);
            if (ev is null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserID != organiser.OrganiserID) return Forbidden("You can only change your own events.");

            var problem = CheckImage(file);
            if (problem is not null) return BadRequest(new { message = problem });

            try
            {
                ev.BannerImageUrl = await _blobs.UploadImageAsync(file, "banners");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is RequestFailedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not available right now." });
            }

            await Db.SaveChangesAsync();
            return Ok(new { ev.EventID, ev.BannerImageUrl });
        }

        /// <summary>Uploads a profile picture for the logged-in Participant.</summary>
        /// <remarks>Send the image as multipart/form-data in a field called "file". JPG, PNG or WEBP, up to 5 MB.</remarks>
        /// <param name="file">The profile picture.</param>
        /// <response code="200">Picture saved. Returns the image URL.</response>
        /// <response code="400">No file, wrong file type, or the file is too big.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="503">Image storage is not available.</response>
        [HttpPost("users/me/profile-picture")]
        [Consumes("multipart/form-data")]
        [RequireRole("Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var participant = await CurrentParticipantAsync();
            if (participant is null) return Forbidden("Participant profile not found.");

            var problem = CheckImage(file);
            if (problem is not null) return BadRequest(new { message = problem });

            try
            {
                participant.ProfilePictureUrl = await _blobs.UploadImageAsync(file, "profiles");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is RequestFailedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not available right now." });
            }

            await Db.SaveChangesAsync();
            return Ok(new { participant.ProfilePictureUrl });
        }

        /// <summary>Returns an error message if the file is not an acceptable image, otherwise null.</summary>
        private static string? CheckImage(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return "Please choose an image file.";
            if (file.Length > MaxFileBytes)
                return "The image must be 5 MB or smaller.";

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return "Only JPG, PNG or WEBP images are allowed.";

            return null;
        }
    }
}
