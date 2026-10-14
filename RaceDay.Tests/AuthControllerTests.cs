using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Tests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest("Jane Doe", $"jane{Guid.NewGuid()}@test.com", "Password123!", "Participant", null);

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"dup{Guid.NewGuid()}@test.com";
        var request = new RegisterRequest("Jane Doe", email, "Password123!", "Participant", null);

        await client.PostAsJsonAsync("/api/auth/register", request);
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest("Jane Doe", $"bad{Guid.NewGuid()}@test.com", "Password123!", "Admin", null);

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = $"login{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("John Smith", email, "Password123!", "Participant", null));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"wrongpw{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("John Smith", email, "Password123!", "Participant", null));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
