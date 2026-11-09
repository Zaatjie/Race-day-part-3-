using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole(Roles.Participant)]
public class ParticipantController : AppController
{
    private readonly IRaceDayApi _api;
    public ParticipantController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Enrolments()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success) Failure(result.Error ?? "We couldn't load your enrolments.");
        return View(result.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await _api.CancelEnrolmentAsync(id);
        if (ExpiredRedirect(result) is { } redirect) return redirect;

        if (result.Success) Success("Your enrolment has been cancelled.");
        else Failure(result.Error ?? "We couldn't cancel that enrolment.");
        return RedirectToAction(nameof(Enrolments));
    }

    [HttpGet]
    public async Task<IActionResult> Results()
    {
        var result = await _api.GetMyResultsAsync();
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success) Failure(result.Error ?? "We couldn't load your results.");
        return View(result.Data ?? new List<ResultDto>());
    }
}
