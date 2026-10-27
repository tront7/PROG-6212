using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared behaviour for every controller in the MVC app.</summary>
public abstract class AppController : Controller
{
    protected readonly IApiClient Api;

    protected AppController(IApiClient api)
    {
        Api = api;
    }

    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected void FlashSuccess(string message) => TempData["Success"] = message;
    protected void FlashError(string message) => TempData["Error"] = message;

    /// <summary>Sends each role to its own landing page.</summary>
    protected IActionResult RedirectToRoleHome() =>
        HttpContext.Session.CurrentRole() == "Organiser"
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Participant");

    /// <summary>
    /// If the API says the session has ended (401) or the role isn't allowed (403), returns the
    /// redirect to send the user to. Returns null when the call was not an auth failure.
    /// </summary>
    protected IActionResult? RedirectOnAuthFailure<T>(ApiResult<T> result)
    {
        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            HttpContext.Session.Clear();
            FlashError("Your session has expired. Please log in again.");
            return RedirectToAction("Login", "Account");
        }

        if (result.StatusCode == HttpStatusCode.Forbidden)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return null;
    }
}
