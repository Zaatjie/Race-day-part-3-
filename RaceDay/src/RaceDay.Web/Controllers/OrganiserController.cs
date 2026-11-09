using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserController : OrganiserControllerBase
{
    public OrganiserController(IRaceDayApi api) : base(api) { }

    /// <summary>Summary of the organiser's events, enrolments per event and upcoming dates.</summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var events = await LoadMyEventsAsync();
        if (ExpiredRedirect(events) is { } redirect) return redirect;

        var vm = new DashboardViewModel();
        if (!events.Success)
        {
            Failure(events.Error ?? "We couldn't load your events.");
            return View(vm);
        }

        var counts = await Task.WhenAll(events.Data!.Select(async e =>
        {
            var enrolments = await Api.GetEventEnrolmentsAsync(e.EventId);
            return new DashboardEventRow
            {
                Event = e,
                EnrolmentCount = enrolments.Success ? enrolments.Data!.Count : 0
            };
        }));
        vm.Rows = counts.ToList();
        return View(vm);
    }
}
