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
}
