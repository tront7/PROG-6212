using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Everything a Participant can do. The whole controller is restricted to the Participant role.</summary>
[SessionAuthorize("Participant")]
public class ParticipantController : AppController
{
    public ParticipantController(IApiClient api) : base(api) { }

    // =====================================================================
    // Home: upcoming events with filters
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilterViewModel filter)
    {
        var query = new Dictionary<string, string?> { ["upcomingOnly"] = "true" };
        if (!string.IsNullOrWhiteSpace(filter.Search)) query["search"] = filter.Search.Trim();
        if (!string.IsNullOrWhiteSpace(filter.EventType)) query["eventType"] = filter.EventType;
        if (!string.IsNullOrWhiteSpace(filter.Location)) query["location"] = filter.Location.Trim();
        if (filter.From.HasValue) query["from"] = filter.From.Value.ToString("yyyy-MM-dd");
        if (filter.To.HasValue) query["to"] = filter.To.Value.ToString("yyyy-MM-dd");

        var result = await Api.GetAsync<List<EventDto>>(QueryHelpers.AddQueryString("api/events", query));
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success) ViewData["Error"] = result.Error;

        return View(new ParticipantHomeViewModel
        {
            Filter = filter,
            Events = result.Data ?? new List<EventDto>()
        });
    }

    // =====================================================================
    // Event details + enter event
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> EventDetails(int id)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (ev.IsNotFound)
        {
            FlashError("That event could not be found. It may have been removed.");
            return RedirectToAction(nameof(Index));
        }

        if (!ev.Success || ev.Data is null)
        {
            FlashError(ev.Error ?? "We couldn't load that event.");
            return RedirectToAction(nameof(Index));
        }

        var categories = await Api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        var mine = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/me");

        var redirect = RedirectOnAuthFailure(mine);
        if (redirect is not null) return redirect;

        if (!categories.Success) ViewData["Error"] = categories.Error;

        return View(new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new List<CategoryDto>(),
            MyEnrolments = (mine.Data ?? new List<EnrolmentDto>()).Where(e => e.EventId == id).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await Api.PostAsync<EnrolmentDto>("api/enrolments", new { eventId, categoryId });
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success)
        {
            FlashError(result.Error ?? "We couldn't enter you into that event.");
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        FlashSuccess("You're in! Your entry has been recorded. Good luck!");
        return RedirectToAction(nameof(MyEvents));
    }

    // =====================================================================
    // My events
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        var result = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/me");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success) ViewData["Error"] = result.Error;

        return View(result.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await Api.DeleteAsync($"api/enrolments/{id}");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (result.Success) FlashSuccess("Your entry has been cancelled.");
        else FlashError(result.Error ?? "We couldn't cancel that entry.");

        return RedirectToAction(nameof(MyEvents));
    }

    // =====================================================================
    // My results
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var result = await Api.GetAsync<List<MyResultDto>>("api/results/me");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success) ViewData["Error"] = result.Error;

        return View(result.Data ?? new List<MyResultDto>());
    }
}
