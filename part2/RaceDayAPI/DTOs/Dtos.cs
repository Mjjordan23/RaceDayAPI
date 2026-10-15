namespace RaceDayAPI.DTOs
{
    // ---------- Auth ----------
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"

        // Participant fields
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }

        // Organiser fields
        public string? OrganisationName { get; set; }
        public string? ContactNumber { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // ---------- Events ----------
    public class EventCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
    }

    // ---------- Categories ----------
    public class CategoryCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    // ---------- Enrolments ----------
    public class EnrolmentCreateDto
    {
        public int CategoryId { get; set; }
    }

    // ---------- Results ----------
    public class ResultCreateDto
    {
        public TimeSpan FinishTime { get; set; }
        public int FinishPosition { get; set; }
    }
}
