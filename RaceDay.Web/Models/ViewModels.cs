using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// ---------- Account ----------

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(8, ErrorMessage = "Your password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose how you'll use RaceDay.")]
    public string Role { get; set; } = "Participant";
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? PictureFile { get; set; }

    // Display-only (filled in by the controller)
    public string? ProfilePictureUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
}

// ---------- Organiser ----------

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "The description can be at most 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date and start time.")]
    [Display(Name = "Date and start time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    public string? ExistingBannerUrl { get; set; }

    public bool IsEdit => EventId > 0;
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(50)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000, ErrorMessage = "Price cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal Price { get; set; }

    public bool IsEdit => CategoryId > 0;
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    public TimeSpan? FinishTime { get; set; }

    [Range(1, 1000000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

public class OrganiserDashboardViewModel
{
    public List<EventSummaryDto> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.TotalEnrolments);
    public int UpcomingCount => Events.Count(e => e.IsUpcoming);
    public List<EventSummaryDto> Upcoming => Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).Take(5).ToList();
}

public class EventPickerViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetAction { get; set; } = string.Empty;
    public string ButtonText { get; set; } = "Open";
    public List<EventSummaryDto> Events { get; set; } = new();
}

public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = null!;
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public EventDto Event { get; set; } = null!;
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

// ---------- Participant ----------

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Location { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType) ||
        !string.IsNullOrWhiteSpace(Location) || From.HasValue || To.HasValue;
}

public class ParticipantHomeViewModel
{
    public EventFilterViewModel Filter { get; set; } = new();
    public List<EventDto> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = null!;
    public List<CategoryDto> Categories { get; set; } = new();
    public List<EnrolmentDto> MyEnrolments { get; set; } = new();

    public EnrolmentDto? EnrolmentFor(int categoryId) => MyEnrolments.FirstOrDefault(e => e.CategoryId == categoryId);
    public bool IsPast => Event.EventDate < DateTime.UtcNow.Date;
}

// ---------- Shared partials ----------

public record BannerModel(string? Url, string EventType, string CssClass = "");
