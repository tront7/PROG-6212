using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects a controller or action using the session. Users who are not logged in are sent
/// to the login page; users with the wrong role are sent to an "access denied" page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsAuthenticated())
        {
            var request = context.HttpContext.Request;
            var returnUrl = HttpMethods.IsGet(request.Method) ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_requiredRole is not null && !string.Equals(session.CurrentRole(), _requiredRole, StringComparison.Ordinal))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
