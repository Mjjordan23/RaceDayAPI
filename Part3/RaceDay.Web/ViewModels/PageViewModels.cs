using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.ViewModels
{
    // ---------- Account ----------

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please repeat your password.")]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a role.")]
        public string Role { get; set; } = "Participant";

        // Participant fields
        [StringLength(50)]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        // Organiser fields
        [StringLength(100)]
        [Display(Name = "Organisation name")]
        public string? OrganisationName { get; set; }

        [StringLength(20)]
        [Display(Name = "Contact number")]
        public string? ContactNumber { get; set; }
    }

    public class ProfileViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Organisation name")]
        public string? OrganisationName { get; set; }

        [StringLength(20)]
        [Display(Name = "Contact number")]
        public string? ContactNumber { get; set; }
    }

    // ---------- Participant ----------

    public class HomeViewModel
    {
        public string? Type { get; set; }
        public string? Search { get; set; }
        public bool UpcomingOnly { get; set; } = true;
        public List<EventDto> Events { get; set; } = new();
    }

    public class EventDetailsViewModel
    {
        public EventDetailDto Event { get; set; } = new();

        /// <summary>The logged-in Participant's enrolment in this event, if they already entered.</summary>
        public MyEnrolmentDto? MyEnrolment { get; set; }
    }

    // ---------- Organiser ----------

    public class DashboardViewModel
    {
        public List<MyEventDto> Events { get; set; } = new();
        public int TotalEvents => Events.Count;
        public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
        public int UpcomingCount => Events.Count(e => e.IsUpcoming);
        public MyEventDto? NextEvent => Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).FirstOrDefault();
    }

    /// <summary>Base for pages that work on one of the Organiser's events chosen from a drop-down.</summary>
    public class EventScopedViewModel
    {
        public List<MyEventDto> Events { get; set; } = new();
        public int? SelectedEventId { get; set; }
        public MyEventDto? SelectedEvent => Events.FirstOrDefault(e => e.EventID == SelectedEventId);
    }

    public class EventFormViewModel
    {
        public int EventID { get; set; }

        [Required(ErrorMessage = "Please enter the event name.")]
        [StringLength(100, ErrorMessage = "The name must be 100 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe the event.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose the date and start time.")]
        [Display(Name = "Date and start time")]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "Please enter the location.")]
        [StringLength(100, ErrorMessage = "The location must be 100 characters or fewer.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the distance.")]
        [Range(0.1, 999, ErrorMessage = "The distance must be between 0.1 and 999 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        [Required(ErrorMessage = "Please choose the event type.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";

        public string? BannerImageUrl { get; set; }

        [Display(Name = "Banner image")]
        public IFormFile? Banner { get; set; }
    }

    public class CategoryFormViewModel
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }

        [Required(ErrorMessage = "Please enter the category name.")]
        [StringLength(50, ErrorMessage = "The name must be 50 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
        [Display(Name = "Minimum age")]
        public int? MinAge { get; set; }

        [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
        [Display(Name = "Maximum age")]
        public int? MaxAge { get; set; }

        [Range(0.1, 999, ErrorMessage = "The distance must be between 0.1 and 999 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }
    }

    public class CategoriesPageViewModel : EventScopedViewModel
    {
        public List<CategoryDto> Categories { get; set; } = new();
        public CategoryFormViewModel NewCategory { get; set; } = new();
    }

    public class EnrolmentsPageViewModel : EventScopedViewModel
    {
        public List<EventEnrolmentDto> Enrolments { get; set; } = new();
    }

    public class ResultRowViewModel
    {
        public EventEnrolmentDto Enrolment { get; set; } = new();
        public EventResultDto? Result { get; set; }
    }

    public class ResultsPageViewModel : EventScopedViewModel
    {
        public List<ResultRowViewModel> Rows { get; set; } = new();
    }
}
