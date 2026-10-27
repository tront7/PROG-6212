using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    public HomeController(IApiClient api) : base(api) { }

    /// <summary>Public landing page. Signed-in users go straight to their own home.</summary>
    public IActionResult Index()
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToRoleHome();
        return View();
    }

    public IActionResult Error(int? code)
    {
        ViewData["Code"] = code;
        ViewData["Message"] = code == 404
            ? "We couldn't find that page."
            : "Something went wrong on our side. Please try again.";
        return View();
    }
}
