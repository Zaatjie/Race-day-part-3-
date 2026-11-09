using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole]
public class ProfileController : AppController
{
    private readonly IRaceDayApi _api;
    public ProfileController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMeAsync();
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success)
        {
            Failure(result.Error ?? "We couldn't load your profile.");
            return HomeForRole();
        }
        return View(ToViewModel(result.Data!));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await FillReadOnlyAsync(vm);
            return View("Index", vm);
        }

        var result = await _api.UpdateMeAsync(new UpdateProfileRequest(vm.FullName.Trim(), vm.Phone));
        if (ExpiredRedirect(result) is { } redirect) return redirect;
        if (!result.Success)
        {
            AddApiErrors(result);
            await FillReadOnlyAsync(vm);
            return View("Index", vm);
        }

        HttpContext.Session.UpdateProfile(result.Data!);
        Success("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        var problem = ImageRules.Validate(picture);
        if (problem is not null)
        {
            Failure(problem);
            return RedirectToAction(nameof(Index));
        }

        var upload = await _api.UploadProfilePictureAsync(picture!);
        if (ExpiredRedirect(upload) is { } redirect) return redirect;
        if (!upload.Success)
        {
            Failure(upload.Error ?? "We couldn't upload your picture.");
            return RedirectToAction(nameof(Index));
        }

        var me = await _api.GetMeAsync();
        if (me.Success) HttpContext.Session.UpdateProfile(me.Data!);
        Success("Your profile picture has been updated.");
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(UserDto u) => new()
    {
        FullName = u.FullName, Phone = u.Phone, Email = u.Email, Role = u.Role,
        ProfilePictureUrl = u.ProfilePictureUrl, MemberSince = u.CreatedAt
    };

    /// <summary>Re-loads the fields the form doesn't post back (email, picture) when redisplaying it.</summary>
    private async Task FillReadOnlyAsync(ProfileViewModel vm)
    {
        var me = await _api.GetMeAsync();
        if (!me.Success) return;
        vm.Email = me.Data!.Email;
        vm.Role = me.Data.Role;
        vm.ProfilePictureUrl = me.Data.ProfilePictureUrl;
        vm.MemberSince = me.Data.CreatedAt;
    }
}
