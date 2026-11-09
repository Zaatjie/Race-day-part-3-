namespace RaceDay.Web.Models;

// These classes only describe the JSON the Part 2 API sends and receives.
// They hold no business logic: all rules live in the API.

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EventTypes
{
    public static readonly string[] All = { "Run", "Walk", "Cycle" };
}

public static class ResultStatuses
{
    public static readonly string[] All = { "Finished", "DNF", "DNS" };
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public UserDto User { get; set; } = new();
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Distance { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string? OrganiserName { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public string EventType { get; set; } = "";
    public decimal Distance { get; set; }
    public string? BannerUrl { get; set; }
    public List<CategoryDto>? Categories { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public string Status { get; set; } = "";
    public int TotalFinishers { get; set; }
}

public class ImageUploadResponse
{
    public string Url { get; set; } = "";
}

// ---- Request bodies sent to the API ----
public record RegisterRequest(string FullName, string Email, string Password, string Role, string? OrganiserCode);
public record LoginRequest(string Email, string Password);
public record UpdateProfileRequest(string FullName, string? Phone);
public record EventRequest(string EventName, string EventDate, string Location, string? Description, string EventType, decimal Distance);
public record CategoryRequest(string CategoryName, decimal Distance, int MaxParticipants, decimal EntryFee);
public record ResultRequest(string FinishTime, int Position, string Status);
