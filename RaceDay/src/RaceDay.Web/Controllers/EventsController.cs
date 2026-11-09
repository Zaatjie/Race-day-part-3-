using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : AppController
{
    private readonly IRaceDayApi _api;
    public EventsController(IRaceDayApi api) => _api = api;

    /// <summary>Full event information and available categories (public).</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (result.NotFound) return NotFound();
        if (!result.Success)
        {
            Failure(result.Error ?? "We couldn't load that event.");
            return RedirectToAction("Index", "Home");
        }

        var vm = new EventDetailsViewModel { Event = result.Data! };

        if (HttpContext.Session.GetRole() == Roles.Participant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            if (ExpiredRedirect(mine) is { } redirect) return redirect;
            if (mine.Success)
                vm.EnrolledCategoryIds = mine.Data!.Select(e => e.CategoryId).ToHashSet();
        }
        return View(vm);
    }

    /// <summary>Enrols the logged-in participant in the chosen category (recorded immediately).</summary>
    [HttpPost, RequireRole(Roles.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId is null)
        {
            Failure("Please select a category before enrolling.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.EnrolAsync(categoryId.Value);
        if (ExpiredRedirect(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            Failure(result.Error ?? "We couldn't complete your enrolment.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Success("You're in! Your enrolment has been confirmed.");
        return RedirectToAction("Enrolments", "Participant");
    }
}
