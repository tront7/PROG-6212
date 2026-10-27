using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

// Use one fixed culture everywhere so decimals ("10.5") and dates parse and display the same on
// every machine (some Windows regional settings use a comma as the decimal separator).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST form is protected against cross-site request forgery.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Server-side session: stores the signed-in user's id, role and display details.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = "RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpContextAccessor();

// Typed HTTP client for the RaceDay API (Part 2). Cookies are handled manually per user
// (see ApiClient), so the shared handler must not keep its own cookie jar.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000/";
if (!apiBaseUrl.EndsWith('/')) apiBaseUrl += "/";

builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
