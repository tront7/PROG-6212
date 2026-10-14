using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Tests;

public class EventsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EventsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEvents_WithoutAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(
            _factory, $"organiser{Guid.NewGuid()}@test.com", "Password123!", "Organiser");

        var request = new CreateEventRequest("Test Marathon", DateTime.UtcNow.AddMonths(1), "Cape Town", "A test event.");
        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(
            _factory, $"participant{Guid.NewGuid()}@test.com", "Password123!", "Participant");

        var request = new CreateEventRequest("Test Marathon", DateTime.UtcNow.AddMonths(1), "Cape Town", "A test event.");
        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new CreateEventRequest("Test Marathon", DateTime.UtcNow.AddMonths(1), "Cape Town", "A test event.");
        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
