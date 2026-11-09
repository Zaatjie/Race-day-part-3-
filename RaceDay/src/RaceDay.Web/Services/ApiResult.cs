using System.Net;

namespace RaceDay.Web.Services;

/// <summary>The outcome of a call to the RaceDay API, with a friendly message on failure.</summary>
public class ApiResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, string[]> FieldErrors { get; init; } = new();
    public HttpStatusCode? Status { get; init; }
    public bool SessionExpired { get; init; }
    public bool NotFound => Status == HttpStatusCode.NotFound;
    public bool Forbidden => Status == HttpStatusCode.Forbidden;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}
