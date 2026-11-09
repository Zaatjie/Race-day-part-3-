using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Blocks unauthenticated users (redirect to Login) and users with the wrong role (Access denied).
/// Pass no role to allow any logged-in user.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string? _role;
    public RequireRoleAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (!session.IsSignedIn())
        {
            var req = context.HttpContext.Request;
            var returnUrl = HttpMethods.IsGet(req.Method) ? req.Path + req.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_role is not null && session.GetRole() != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
