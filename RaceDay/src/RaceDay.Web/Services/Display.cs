namespace RaceDay.Web.Services;

/// <summary>Formatting helpers for views (no business rules).</summary>
public static class Display
{
    public static string Time(TimeSpan t) =>
        $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";

    /// <summary>The API calls a live enrolment "Enrolled"; the UI shows it as Confirmed.</summary>
    public static string EnrolmentLabel(string status) => status switch
    {
        "Enrolled" => "Confirmed",
        _ => status
    };

    public static string EnrolmentClass(string status) => status switch
    {
        "Enrolled" or "Confirmed" => "status-confirmed",
        "Pending" => "status-pending",
        "Cancelled" => "status-cancelled",
        _ => "status-pending"
    };

    public static string ResultClass(string status) => status switch
    {
        "Finished" => "status-confirmed",
        "DNF" => "status-pending",
        _ => "status-cancelled"
    };

    public static string TypeIcon(string type) => type switch
    {
        "Run" => "bi-lightning-charge-fill",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag-fill"
    };

    public static string Ordinal(int n) => n.ToString();
}
