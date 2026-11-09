using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Route("Organiser/Results")]
public class ManageResultsController : OrganiserControllerBase
{
    public ManageResultsController(IRaceDayApi api) : base(api) { }

    /// <summary>Finish-time and position entry for the enrolled participants of one event.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? eventId)
    {
        var events = await LoadMyEventsAsync();
        if (ExpiredRedirect(events) is { } redirect) return redirect;

        var vm = new ResultsPageViewModel { Events = events.Data ?? new() };
        if (!events.Success) Failure(events.Error ?? "We couldn't load your events.");

        var chosen = Pick(vm.Events, eventId);
        if (chosen is null) return View(vm);

        var detail = await Api.GetEventAsync(chosen.EventId);
        vm.Selected = detail.Success ? detail.Data : chosen;
        vm.EventHasHappened = vm.Selected!.EventDate.Date <= DateTime.Today;
        if (!vm.EventHasHappened) return View(vm);

        var enrolments = await Api.GetEventEnrolmentsAsync(chosen.EventId);
        if (ExpiredRedirect(enrolments) is { } redirect2) return redirect2;
        if (!enrolments.Success)
        {
            Failure(enrolments.Error ?? "We couldn't load the enrolments.");
            return View(vm);
        }

        // Existing results per category, so rows can show what has already been published.
        var existing = new Dictionary<int, ResultDto>();
        foreach (var cat in vm.Selected.Categories ?? new List<CategoryDto>())
        {
            var board = await Api.GetLeaderboardAsync(cat.CategoryId);
            if (board.Success)
                foreach (var r in board.Data!) existing[r.EnrolmentId] = r;
        }

        vm.Rows = enrolments.Data!
            .Where(e => e.Status == "Enrolled")
            .OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName)
            .Select(e => new ResultRow { Enrolment = e, Result = existing.GetValueOrDefault(e.EnrolmentId) })
            .ToList();
        return View(vm);
    }

    /// <summary>Captures (or updates) and publishes one participant's result.</summary>
    [HttpPost("Save")]
    public async Task<IActionResult> Save(ResultFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            Failure(first ?? "Please check the result details and try again.");
            return RedirectToAction(nameof(Index), new { eventId = form.EventId });
        }

        if (form.Status == "Finished" && form.Position < 1)
        {
            Failure("Please enter a finishing position of 1 or more for a finisher.");
            return RedirectToAction(nameof(Index), new { eventId = form.EventId });
        }

        if (!TimeSpan.TryParse(form.FinishTime, CultureInfo.InvariantCulture, out var time))
        {
            Failure("Finish time must look like 01:23:45 (hh:mm:ss).");
            return RedirectToAction(nameof(Index), new { eventId = form.EventId });
        }
        var body = new ResultRequest(time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture), form.Position, form.Status);

        var result = form.ResultId.HasValue
            ? await Api.UpdateResultAsync(form.ResultId.Value, body)
            : await Api.CaptureResultAsync(form.EnrolmentId, body);
        if (ExpiredRedirect(result) is { } redirect) return redirect;

        if (result.Success) Success("Result published.");
        else
        {
            var detail = result.FieldErrors.Values.SelectMany(v => v).FirstOrDefault();
            Failure(detail ?? result.Error ?? "We couldn't save that result.");
        }
        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }
}
