using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// ---------- Account ----------
public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like a valid email address."), StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters long."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose a role.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = Roles.Participant;

    [Display(Name = "Organiser code")]
    public string? OrganiserCode { get; set; }
}

// ---------- Participant ----------
public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Location { get; set; }
    public string? Type { get; set; }
    public DateTime? Date { get; set; }
    public bool ApiUnavailable { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
}

// ---------- Organiser ----------
public class DashboardEventRow
{
    public EventDto Event { get; set; } = new();
    public int EnrolmentCount { get; set; }
}

public class DashboardViewModel
{
    public List<DashboardEventRow> Rows { get; set; } = new();
    public int TotalEvents => Rows.Count;
    public int TotalEnrolments => Rows.Sum(r => r.EnrolmentCount);
    public List<DashboardEventRow> Upcoming => Rows
        .Where(r => r.Event.EventDate.Date >= DateTime.Today)
        .OrderBy(r => r.Event.EventDate).ToList();
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter an event name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = "";

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = "";

    [StringLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    public string? ExistingBannerUrl { get; set; }
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name."), StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = "";

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Range(1, 100000, ErrorMessage = "Max participants must be at least 1.")]
    [Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 999999.99, ErrorMessage = "Entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }
}

public class CategoriesPageViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public int? CategoryId { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsPageViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public bool EventHasHappened { get; set; }
    public List<ResultRow> Rows { get; set; } = new();
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    [Required(ErrorMessage = "Please enter the finish time as hh:mm:ss.")]
    [RegularExpression(@"^([01]?\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Finish time must look like 01:23:45 (hh:mm:ss).")]
    public string FinishTime { get; set; } = "";

    [Range(0, 100000, ErrorMessage = "Position must be 0 or more.")]
    public int Position { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

// ---------- Profile ----------
public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [StringLength(20, ErrorMessage = "Phone number can be at most 20 characters.")]
    public string? Phone { get; set; }

    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
    public DateTime MemberSince { get; set; }
}

public class ErrorViewModel
{
    public int? StatusCode { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "An unexpected error occurred. Please try again.";
}

public class EventPickerModel
{
    public List<EventDto> Events { get; set; } = new();
    public int? SelectedId { get; set; }
    /// <summary>The controller whose Index action the picker submits to.</summary>
    public string Controller { get; set; } = "";
}
