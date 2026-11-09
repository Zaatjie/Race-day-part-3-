using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Route("Organiser/Categories")]
public class ManageCategoriesController : OrganiserControllerBase
{
    public ManageCategoriesController(IRaceDayApi api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? eventId)
    {
        var vm = await BuildPageAsync(eventId, new CategoryFormViewModel());
        if (vm is null) return RedirectToAction("Login", "Account");
        return View(vm);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var page = await BuildPageAsync(form.EventId, form);
            if (page is null) return RedirectToAction("Login", "Account");
            return View("Index", page);
        }

        var result = await Api.CreateCategoryAsync(form.EventId, ToRequest(form));
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success)
        {
            AddApiErrors(result);
            var page = await BuildPageAsync(form.EventId, form);
            if (page is null) return RedirectToAction("Login", "Account");
            return View("Index", page);
        }

        Success("Category added.");
        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }

    [HttpGet("Edit/{eventId:int}/{id:int}")]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var ev = await Api.GetEventAsync(eventId);
        if (ExpiredRedirect(ev) is { } redirect) return redirect;
        var cat = ev.Success && ev.Data!.OrganiserId == CurrentUserId
            ? ev.Data.Categories?.FirstOrDefault(c => c.CategoryId == id)
            : null;
        if (cat is null)
        {
            Failure("We couldn't find that category.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        ViewBag.EventName = ev.Data!.EventName;
        return View(new CategoryFormViewModel
        {
            CategoryId = cat.CategoryId, EventId = eventId, CategoryName = cat.CategoryName,
            Distance = cat.Distance, MaxParticipants = cat.MaxParticipants, EntryFee = cat.EntryFee
        });
    }

    [HttpPost("Edit/{eventId:int}/{id:int}")]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel form)
    {
        form.EventId = eventId;
        form.CategoryId = id;
        if (!ModelState.IsValid) return View(form);

        var result = await Api.UpdateCategoryAsync(id, ToRequest(form));
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success)
        {
            AddApiErrors(result);
            return View(form);
        }

        Success("Category updated.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost("Delete/{eventId:int}/{id:int}")]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (ExpiredRedirect(result) is { } redirect) return redirect;

        if (result.Success) Success("Category removed.");
        else Failure(result.Error ?? "We couldn't remove that category.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    /// <summary>Returns null when the session expired (caller redirects to Login).</summary>
    private async Task<CategoriesPageViewModel?> BuildPageAsync(int? eventId, CategoryFormViewModel form)
    {
        var events = await LoadMyEventsAsync();
        if (events.SessionExpired) { Failure("Your session has expired. Please log in again."); return null; }

        var vm = new CategoriesPageViewModel { Events = events.Data ?? new(), Form = form };
        if (!events.Success) Failure(events.Error ?? "We couldn't load your events.");

        var chosen = Pick(vm.Events, eventId);
        if (chosen is not null)
        {
            var detail = await Api.GetEventAsync(chosen.EventId);
            vm.Selected = detail.Success ? detail.Data : chosen;
            vm.Form.EventId = chosen.EventId;
        }
        return vm;
    }

    private static CategoryRequest ToRequest(CategoryFormViewModel f) =>
        new(f.CategoryName.Trim(), f.Distance, f.MaxParticipants, f.EntryFee);
}
