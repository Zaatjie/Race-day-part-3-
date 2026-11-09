using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    private readonly IRaceDayApi _api;
    public HomeController(IRaceDayApi api) => _api = api;

    /// <summary>Upcoming events with filtering. Organisers are sent to their dashboard instead.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? location, string? type, DateTime? date)
    {
        if (HttpContext.Session.GetRole() == Roles.Organiser)
            return RedirectToAction("Dashboard", "Organiser");

        var vm = new HomeViewModel { Location = location, Type = type, Date = date };
        var result = await _api.GetEventsAsync(date, location);
        if (!result.Success)
        {
            vm.ApiUnavailable = true;
            return View(vm);
        }

        var events = result.Data!.Where(e => e.EventDate.Date >= DateTime.Today);
        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType == type);
        vm.Events = events.OrderBy(e => e.EventDate).ToList();
        return View(vm);
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Error(int? id)
    {
        var vm = id switch
        {
            404 => new ErrorViewModel
            {
                StatusCode = 404, Title = "Page not found",
                Message = "We couldn't find the page you were looking for."
            },
            403 => new ErrorViewModel
            {
                StatusCode = 403, Title = "Access denied",
                Message = "You don't have permission to view that page."
            },
            _ => new ErrorViewModel { StatusCode = id }
        };
        Response.StatusCode = id ?? 500;
        return View(vm);
    }
}
