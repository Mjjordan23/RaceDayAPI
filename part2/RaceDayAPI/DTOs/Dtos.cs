namespace RaceDayAPI.DTOs
{
    // ---------- Auth ----------

    /// <summary>Details needed to create an account. Which extra fields are required depends on the role.</summary>
    public class RegisterDto
    {
        /// <summary>Login email. Must be unique.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Password, at least 8 characters. It is hashed before it is stored.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Either "Organiser" or "Participant".</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Participants only: first name.</summary>
        public string? FirstName { get; set; }

        /// <summary>Participants only: last name.</summary>
        public string? LastName { get; set; }

        /// <summary>Participants only: date of birth.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Participants only (optional): phone number.</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Organisers only: name of the organisation running the events.</summary>
        public string? OrganisationName { get; set; }

        /// <summary>Organisers only (optional): contact number.</summary>
        public string? ContactNumber { get; set; }
    }

    /// <summary>Credentials used to log in.</summary>
    public class LoginDto
    {
        /// <summary>The email used to register.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>The account password.</summary>
        public string Password { get; set; } = string.Empty;
    }

    // ---------- Profile ----------

    /// <summary>The logged-in user's profile. Fields for the other role are null.</summary>
    public class UserProfileDto
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int? ParticipantID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public int? OrganiserID { get; set; }
        public string? OrganisationName { get; set; }
        public string? ContactNumber { get; set; }
    }

    /// <summary>Profile fields to change. Anything left out (null) is not changed.</summary>
    public class UpdateProfileDto
    {
        /// <summary>Participants only.</summary>
        public string? FirstName { get; set; }

        /// <summary>Participants only.</summary>
        public string? LastName { get; set; }

        /// <summary>Participants only.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Participants only.</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Organisers only.</summary>
        public string? OrganisationName { get; set; }

        /// <summary>Organisers only.</summary>
        public string? ContactNumber { get; set; }
    }

    // ---------- Events ----------

    /// <summary>The details of an event. Used to create or update an event.</summary>
    public class EventCreateDto
    {
        /// <summary>Event name, up to 100 characters.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>What the event is about.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Start date and time of the event.</summary>
        public DateTime EventDate { get; set; }

        /// <summary>Where the event is held, up to 100 characters.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Total distance in kilometres. Greater than 0 and below 1000.</summary>
        public decimal DistanceKm { get; set; }

        /// <summary>Either "Run", "Walk" or "Cycle".</summary>
        public string EventType { get; set; } = string.Empty;
    }

    // ---------- Categories ----------

    /// <summary>An age or distance category within an event, such as "Under 20" or "21km".</summary>
    public class CategoryCreateDto
    {
        /// <summary>Category name, up to 50 characters.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional minimum age.</summary>
        public int? MinAge { get; set; }

        /// <summary>Optional maximum age. Must not be below MinAge.</summary>
        public int? MaxAge { get; set; }

        /// <summary>Optional distance for this category in kilometres.</summary>
        public decimal? DistanceKm { get; set; }
    }

    // ---------- Enrolments ----------

    /// <summary>The category a Participant wants to enter the event under.</summary>
    public class EnrolmentCreateDto
    {
        /// <summary>The id of a category that belongs to the event being entered.</summary>
        public int CategoryId { get; set; }
    }

    // ---------- Results ----------

    /// <summary>A Participant's finish time and position, captured by the Organiser after the event.</summary>
    public class ResultCreateDto
    {
        /// <summary>Finish time as hh:mm:ss, for example "03:45:30".</summary>
        public TimeSpan FinishTime { get; set; }

        /// <summary>Finishing position, 1 or higher.</summary>
        public int FinishPosition { get; set; }
    }
}
