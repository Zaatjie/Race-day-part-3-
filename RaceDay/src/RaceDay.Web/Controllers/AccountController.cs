using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    private readonly IRaceDayApi _api;
    public AccountController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (HttpContext.Session.IsSignedIn()) return HomeForRole();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _api.LoginAsync(new LoginRequest(vm.Email.Trim(), vm.Password));
        if (!result.Success)
        {
            AddApiErrors(result);
            return View(vm);
        }

        HttpContext.Session.SignIn(result.Data!);
        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return LocalRedirect(vm.ReturnUrl);
        return HomeForRole();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsSignedIn()) return HomeForRole();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (vm.Role == Roles.Organiser && string.IsNullOrWhiteSpace(vm.OrganiserCode))
            ModelState.AddModelError(nameof(vm.OrganiserCode), "An organiser code is required to register as an Organiser.");
        if (vm.Role != Roles.Organiser && vm.Role != Roles.Participant)
            ModelState.AddModelError(nameof(vm.Role), "Please choose Participant or Organiser.");
        if (!ModelState.IsValid) return View(vm);

        var result = await _api.RegisterAsync(new RegisterRequest(
            vm.FullName.Trim(), vm.Email.Trim(), vm.Password, vm.Role,
            vm.Role == Roles.Organiser ? vm.OrganiserCode : null));

        if (!result.Success)
        {
            AddApiErrors(result);
            return View(vm);
        }

        Success("Your account has been created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Success("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
