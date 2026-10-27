using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public static class TestHelpers
{
    /// <summary>
    /// Registers and logs in a new user, returning an HttpClient that carries
    /// the resulting session cookie on every subsequent request — mirroring
    /// how a real browser session behaves against the cookie-auth API.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory, string email, string password, string role, string fullName = "Test User")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(fullName, email, password, role, null));
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    /// <summary>Builds a multipart/form-data body containing a single file field called "file".</summary>
    public static MultipartFormDataContent FileUpload(string fileName, string contentType)
    {
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    /// <summary>Creates an event (and optionally one category) as the given organiser client.</summary>
    public static async Task<(EventResponse ev, CategoryResponse category)> CreateEventWithCategoryAsync(
        HttpClient organiser, string eventName = "Test Event", string eventType = "Run")
    {
        var eventResponse = await organiser.PostAsJsonAsync("/api/events",
            new CreateEventRequest(eventName, DateTime.UtcNow.AddMonths(1), "Pretoria", "Test event", 10, eventType));
        eventResponse.EnsureSuccessStatusCode();
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{ev!.EventId}/categories",
            new CreateCategoryRequest("10km", 10, 50, 120));
        categoryResponse.EnsureSuccessStatusCode();
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        return (ev, category!);
    }
}
