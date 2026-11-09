using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>Every data operation in the MVC app goes through this interface to the Part 2 API.</summary>
public interface IRaceDayApi
{
    // Auth
    Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest req);
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest req);

    // Events
    Task<ApiResult<List<EventDto>>> GetEventsAsync(DateTime? date = null, string? location = null);
    Task<ApiResult<EventDto>> GetEventAsync(int id);
    Task<ApiResult<EventDto>> CreateEventAsync(EventRequest req);
    Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest req);
    Task<ApiResult> DeleteEventAsync(int id);
    Task<ApiResult<ImageUploadResponse>> UploadBannerAsync(int eventId, IFormFile file);
    Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId);

    // Categories
    Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest req);
    Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int id, CategoryRequest req);
    Task<ApiResult> DeleteCategoryAsync(int id);

    // Enrolments
    Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId);
    Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult> CancelEnrolmentAsync(int enrolmentId);

    // Results
    Task<ApiResult<List<ResultDto>>> GetLeaderboardAsync(int categoryId);
    Task<ApiResult<ResultDto>> CaptureResultAsync(int enrolmentId, ResultRequest req);
    Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, ResultRequest req);
    Task<ApiResult<List<ResultDto>>> GetMyResultsAsync();

    // Profile
    Task<ApiResult<UserDto>> GetMeAsync();
    Task<ApiResult<UserDto>> UpdateMeAsync(UpdateProfileRequest req);
    Task<ApiResult<ImageUploadResponse>> UploadProfilePictureAsync(IFormFile file);
}

public class RaceDayApi : IRaceDayApi
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<RaceDayApi> _log;

    public RaceDayApi(HttpClient client, IHttpContextAccessor http, ILogger<RaceDayApi> log)
    {
        _client = client; _http = http; _log = log;
    }

    // ---------- Auth ----------
    public Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest req) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", req);

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest req) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", req);

    // ---------- Events ----------
    public Task<ApiResult<List<EventDto>>> GetEventsAsync(DateTime? date = null, string? location = null)
    {
        var q = new List<string>();
        if (date.HasValue) q.Add($"date={date.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(location)) q.Add($"location={Uri.EscapeDataString(location.Trim())}");
        var url = "api/events" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
        return SendAsync<List<EventDto>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<EventDto>> GetEventAsync(int id) =>
        SendAsync<EventDto>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<EventDto>> CreateEventAsync(EventRequest req) =>
        SendAsync<EventDto>(HttpMethod.Post, "api/events", req);

    public Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest req) =>
        SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", req);

    public async Task<ApiResult> DeleteEventAsync(int id) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResult<ImageUploadResponse>> UploadBannerAsync(int eventId, IFormFile file) =>
        SendAsync<ImageUploadResponse>(HttpMethod.Post, $"api/events/{eventId}/banner", content: FileContent(file));

    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    // ---------- Categories ----------
    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest req) =>
        SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", req);

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int id, CategoryRequest req) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{id}", req);

    public async Task<ApiResult> DeleteCategoryAsync(int id) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/categories/{id}");

    // ---------- Enrolments ----------
    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId) =>
        SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/users/me/enrolments");

    public async Task<ApiResult> CancelEnrolmentAsync(int enrolmentId) =>
        await SendAsync<object>(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    // ---------- Results ----------
    public Task<ApiResult<List<ResultDto>>> GetLeaderboardAsync(int categoryId) =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/categories/{categoryId}/results");

    public Task<ApiResult<ResultDto>> CaptureResultAsync(int enrolmentId, ResultRequest req) =>
        SendAsync<ResultDto>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", req);

    public Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, ResultRequest req) =>
        SendAsync<ResultDto>(HttpMethod.Put, $"api/results/{resultId}", req);

    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, "api/users/me/results");

    // ---------- Profile ----------
    public Task<ApiResult<UserDto>> GetMeAsync() =>
        SendAsync<UserDto>(HttpMethod.Get, "api/users/me");

    public Task<ApiResult<UserDto>> UpdateMeAsync(UpdateProfileRequest req) =>
        SendAsync<UserDto>(HttpMethod.Put, "api/users/me", req);

    public Task<ApiResult<ImageUploadResponse>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<ImageUploadResponse>(HttpMethod.Post, "api/users/me/picture", content: FileContent(file));

    // ---------- Plumbing ----------
    private static MultipartFormDataContent FileContent(IFormFile file)
    {
        var part = new StreamContent(file.OpenReadStream());
        part.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        return new MultipartFormDataContent { { part, "file", file.FileName } };
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, HttpContent? content = null)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url);
            var token = _http.HttpContext?.Session.GetToken();
            if (!string.IsNullOrEmpty(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (content is not null) req.Content = content;
            else if (body is not null) req.Content = JsonContent.Create(body, options: Json);

            using var res = await _client.SendAsync(req);

            if (res.IsSuccessStatusCode)
            {
                T? data = default;
                if (typeof(T) != typeof(object) && res.StatusCode != HttpStatusCode.NoContent)
                    data = await res.Content.ReadFromJsonAsync<T>(Json);
                return new ApiResult<T> { Success = true, Data = data, Status = res.StatusCode };
            }

            return await FailureAsync<T>(res, token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _log.LogWarning(ex, "API call {Method} {Url} failed", method, url);
            return new ApiResult<T>
            {
                Success = false,
                Error = "We couldn't reach the RaceDay service right now. Please try again in a moment."
            };
        }
    }

    private async Task<ApiResult<T>> FailureAsync<T>(HttpResponseMessage res, string? token)
    {
        if (res.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            // The JWT expired or is no longer valid: end the MVC session.
            _http.HttpContext?.Session.Clear();
            return new ApiResult<T>
            {
                Success = false, Status = res.StatusCode, SessionExpired = true,
                Error = "Your session has expired. Please log in again."
            };
        }

        string? title = null;
        var fieldErrors = new Dictionary<string, string[]>();
        try
        {
            var text = await res.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                        title = t.GetString();
                    if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in errs.EnumerateObject())
                        {
                            if (p.Name.StartsWith('$')) continue; // raw JSON-binding errors are not user friendly
                            fieldErrors[p.Name] = p.Value.EnumerateArray()
                                .Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToArray();
                        }
                    }
                }
            }
        }
        catch (JsonException) { /* body wasn't JSON: fall back to generic text */ }

        var message = res.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Please correct the problems below and try again.",
            HttpStatusCode.Unauthorized => title ?? "Invalid email or password.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => title ?? "That action conflicts with existing data.",
            _ => "Something went wrong on our side. Please try again."
        };

        return new ApiResult<T> { Success = false, Status = res.StatusCode, Error = message, FieldErrors = fieldErrors };
    }
}
