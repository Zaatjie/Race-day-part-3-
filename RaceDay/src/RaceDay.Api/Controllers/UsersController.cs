using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile, enrolments and results.</summary>
[ApiController]
[Route("api/users/me")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;
    public UsersController(RaceDayDbContext db, IBlobStorageService blobs) { _db = db; _blobs = blobs; }

    /// <summary>Gets the logged-in user's profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        return user is null ? NotFound() : UserDto.From(user);
    }

    /// <summary>Updates the logged-in user's full name and phone number.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user is null) return NotFound();
        user.FullName = req.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
        await _db.SaveChangesAsync();
        return UserDto.From(user);
    }

    /// <summary>Lists all enrolments for the logged-in participant.</summary>
    [HttpGet("enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetMyEnrolments()
    {
        var uid = User.GetUserId();
        var list = await _db.Enrolments.WithDetails()
            .Where(e => e.ParticipantId == uid)
            .OrderByDescending(e => e.EnrolmentDate).ToListAsync();
        return list.Select(EnrolmentDto.From).ToList();
    }

    /// <summary>Lists the logged-in participant's own results (added endpoint).</summary>
    [HttpGet("results")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultDto>>> GetMyResults()
    {
        var uid = User.GetUserId();
        var list = await _db.Results.WithDetails()
            .Where(r => r.Enrolment!.ParticipantId == uid).ToListAsync();

        // Total finishers per category, so the client can show "47 of 312".
        var categoryIds = list.Select(r => r.Enrolment!.CategoryId).Distinct().ToList();
        var totals = await _db.Results
            .Where(r => r.Status == ResultStatus.Finished && categoryIds.Contains(r.Enrolment!.CategoryId))
            .GroupBy(r => r.Enrolment!.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return list.OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
                   .Select(r =>
                   {
                       var dto = ResultDto.From(r);
                       dto.TotalFinishers = totals.GetValueOrDefault(r.Enrolment!.CategoryId);
                       return dto;
                   }).ToList();
    }

    /// <summary>Uploads (or replaces) the logged-in user's profile picture. Stored in Azure Blob Storage.</summary>
    /// <remarks>Send multipart/form-data with a single "file" field (JPG, PNG or WEBP, max 5 MB).</remarks>
    [HttpPost("picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImageUploadResponse>> UploadPicture(IFormFile? file)
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user is null) return NotFound();

        var error = BlobStorageService.Validate(file);
        if (error is not null)
        {
            ModelState.AddModelError("file", error);
            return ValidationProblem(ModelState);
        }

        user.ProfilePictureUrl = await _blobs.UploadImageAsync(file!, "profiles");
        await _db.SaveChangesAsync();
        return new ImageUploadResponse { Url = user.ProfilePictureUrl };
    }
}
