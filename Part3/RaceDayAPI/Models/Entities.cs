using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayAPI.Models
{
    public class User
    {
        public int UserID { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Participant? Participant { get; set; }
        public Organiser? Organiser { get; set; }
    }

    public class Participant
    {
        public int ParticipantID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? ProfilePictureUrl { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Organiser
    {
        public int OrganiserID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string OrganisationName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }

    public class Event
    {
        public int EventID { get; set; }

        public int OrganiserID { get; set; }
        public Organiser Organiser { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        [Required, MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DistanceKm { get; set; }

        [Required, MaxLength(20)]
        public string EventType { get; set; } = string.Empty; // "Run", "Walk", "Cycle"

        [MaxLength(255)]
        public string? BannerImageUrl { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Category
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }
        public Event Event { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DistanceKm { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Enrolment
    {
        public int EnrolmentID { get; set; }

        public int ParticipantID { get; set; }
        public Participant Participant { get; set; } = null!;

        public int EventID { get; set; }
        public Event Event { get; set; } = null!;

        public int CategoryID { get; set; }
        public Category Category { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending" or "Confirmed"

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        public Result? Result { get; set; }
    }

    public class Result
    {
        public int ResultID { get; set; }

        public int EnrolmentID { get; set; }
        public Enrolment Enrolment { get; set; } = null!;

        public TimeSpan? FinishTime { get; set; }
        public int? FinishPosition { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
