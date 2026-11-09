using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers for every controller.</summary>
public abstract class AppController : Controller
{
    protected int CurrentUserId => HttpContext.Session.GetUserId();

    protected void Success(string message) => TempData["Success"] = message;
    protected void Failure(string message) => TempData["Error"] = message;

    /// <summary>If the API said the session expired, returns a redirect to Login; otherwise null.</summary>
    protected IActionResult? ExpiredRedirect(ApiResult result)
    {
        if (!result.SessionExpired) return null;
        Failure("Your session has expired. Please log in again.");
        return RedirectToAction("Login", "Account");
    }

    /// <summary>Copies API validation messages into ModelState so forms show them.</summary>
    protected void AddApiErrors(ApiResult result)
    {
        var any = false;
        foreach (var messages in result.FieldErrors.Values)
            foreach (var m in messages) { ModelState.AddModelError(string.Empty, m); any = true; }
        if (!any && !string.IsNullOrEmpty(result.Error))
            ModelState.AddModelError(string.Empty, result.Error);
    }

    protected IActionResult HomeForRole() =>
        HttpContext.Session.GetRole() == Models.Roles.Organiser
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
}
