using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Route("Organiser/Enrolments")]
public class ManageEnrolmentsController : OrganiserControllerBase
{
    public ManageEnrolmentsController(IRaceDayApi api) : base(api) { }

    /// <summary>All participants enrolled for one event, with category and status.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? eventId, int? categoryId)
    {
        var events = await LoadMyEventsAsync();
        if (ExpiredRedirect(events) is { } redirect) return redirect;

        var vm = new EnrolmentsPageViewModel { Events = events.Data ?? new(), CategoryId = categoryId };
        if (!events.Success) Failure(events.Error ?? "We couldn't load your events.");

        var chosen = Pick(vm.Events, eventId);
        if (chosen is null) return View(vm);

        var detail = await Api.GetEventAsync(chosen.EventId);
        vm.Selected = detail.Success ? detail.Data : chosen;

        var enrolments = await Api.GetEventEnrolmentsAsync(chosen.EventId);
        if (ExpiredRedirect(enrolments) is { } redirect2) return redirect2;
        if (!enrolments.Success) Failure(enrolments.Error ?? "We couldn't load the enrolments.");

        var list = enrolments.Data ?? new List<EnrolmentDto>();
        if (categoryId.HasValue) list = list.Where(e => e.CategoryId == categoryId.Value).ToList();
        vm.Enrolments = list.OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName).ToList();
        return View(vm);
    }
}
