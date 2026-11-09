using System.Text.Json.Serialization;

namespace RaceDay.Web.ViewModels
{
    // These classes describe the JSON the API sends back. Names match the API's fields
    // (the JSON reader ignores upper/lower case).

    public class EventDto
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }

        public bool IsUpcoming => EventDate >= DateTime.Now;
    }

    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    public class EventDetailDto : EventDto
    {
        public List<CategoryDto> Categories { get; set; } = new();
    }

    /// <summary>An event owned by the logged-in Organiser, with totals for the dashboard.</summary>
    public class MyEventDto : EventDto
    {
        public int CategoryCount { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class MyEnrolmentDto
    {
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        [JsonPropertyName("event")] public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int CategoryID { get; set; }
        [JsonPropertyName("category")] public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
    }

    public class EventEnrolmentDto
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        [JsonPropertyName("participant")] public string ParticipantName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        [JsonPropertyName("category")] public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
    }

    public class MyResultDto
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        [JsonPropertyName("event")] public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        [JsonPropertyName("category")] public string CategoryName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? FinishPosition { get; set; }
        public int TotalFinishers { get; set; }
    }

    public class EventResultDto
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        [JsonPropertyName("participant")] public string ParticipantName { get; set; } = string.Empty;
        [JsonPropertyName("category")] public string CategoryName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? FinishPosition { get; set; }
    }

    public class LoginResultDto
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ProfileDto
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? OrganisationName { get; set; }
        public string? ContactNumber { get; set; }
    }
}
