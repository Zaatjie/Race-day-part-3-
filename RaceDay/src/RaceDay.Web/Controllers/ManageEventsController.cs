using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Route("Organiser/Events")]
public class ManageEventsController : OrganiserControllerBase
{
    public ManageEventsController(IRaceDayApi api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var events = await LoadMyEventsAsync();
        if (ExpiredRedirect(events) is { } redirect) return redirect;
        if (!events.Success) Failure(events.Error ?? "We couldn't load your events.");
        return View(events.Data ?? new List<EventDto>());
    }

    [HttpGet("Create")]
    public IActionResult Create() =>
        View(new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) });

    [HttpPost("Create")]
    public async Task<IActionResult> Create(EventFormViewModel vm)
    {
        ValidateBanner(vm);
        if (!ModelState.IsValid) return View(vm);

        var result = await Api.CreateEventAsync(ToRequest(vm));
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success)
        {
            AddApiErrors(result);
            return View(vm);
        }

        var newId = result.Data!.EventId;
        if (vm.BannerFile is { Length: > 0 })
        {
            var upload = await Api.UploadBannerAsync(newId, vm.BannerFile);
            if (!upload.Success)
            {
                Failure("Your event was created, but the banner couldn't be uploaded: " + upload.Error);
                return RedirectToAction(nameof(Edit), new { id = newId });
            }
        }

        Success("Event created. Now add some categories to it.");
        return RedirectToAction("Index", "ManageCategories", new { eventId = newId });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success || result.Data!.OrganiserId != CurrentUserId)
        {
            Failure("We couldn't find that event among your events.");
            return RedirectToAction(nameof(Index));
        }

        var e = result.Data;
        return View(new EventFormViewModel
        {
            EventId = e.EventId, EventName = e.EventName, EventDate = e.EventDate.Date,
            Location = e.Location, Description = e.Description, EventType = e.EventType,
            Distance = e.Distance, ExistingBannerUrl = e.BannerUrl
        });
    }

    [HttpPost("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, EventFormViewModel vm)
    {
        vm.EventId = id;
        ValidateBanner(vm);
        if (!ModelState.IsValid) { await KeepBannerAsync(vm); return View(vm); }

        var result = await Api.UpdateEventAsync(id, ToRequest(vm));
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success)
        {
            AddApiErrors(result);
            await KeepBannerAsync(vm);
            return View(vm);
        }

        if (vm.BannerFile is { Length: > 0 })
        {
            var upload = await Api.UploadBannerAsync(id, vm.BannerFile);
            if (!upload.Success)
            {
                Failure("Your changes were saved, but the banner couldn't be uploaded: " + upload.Error);
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        Success("Event updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (ExpiredRedirect(result) is { } redirect) return redirect;

        if (result.Success) Success("Event deleted.");
        else Failure(result.Error ?? "We couldn't delete that event.");
        return RedirectToAction(nameof(Index));
    }

    private void ValidateBanner(EventFormViewModel vm)
    {
        if (vm.BannerFile is { Length: > 0 })
        {
            var problem = ImageRules.Validate(vm.BannerFile);
            if (problem is not null) ModelState.AddModelError(nameof(vm.BannerFile), problem);
        }
        if (vm.EventType is not ("Run" or "Walk" or "Cycle"))
            ModelState.AddModelError(nameof(vm.EventType), "Please choose Run, Walk or Cycle.");
    }

    private async Task KeepBannerAsync(EventFormViewModel vm)
    {
        if (vm.EventId == 0) return;
        var current = await Api.GetEventAsync(vm.EventId);
        if (current.Success) vm.ExistingBannerUrl = current.Data!.BannerUrl;
    }

    private static EventRequest ToRequest(EventFormViewModel vm) => new(
        vm.EventName.Trim(), vm.EventDate!.Value.ToString("yyyy-MM-dd"), vm.Location.Trim(),
        string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        vm.EventType, vm.Distance);
}
