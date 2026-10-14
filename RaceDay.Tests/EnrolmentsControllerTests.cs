using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EnrolmentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int eventId, int categoryId)> CreateEventWithCategoryAsync()
    {
        var organiserClient = await TestHelpers.CreateAuthenticatedClientAsync(
            _factory, $"org{Guid.NewGuid()}@test.com", "Password123!", "Organiser");

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Enrolment Test Event", DateTime.UtcNow.AddMonths(1), "Durban", "desc"));
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiserClient.PostAsJsonAsync($"/api/events/{ev!.EventId}/categories",
            new CreateCategoryRequest("10km", 10, 100, 150));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        return (ev.EventId, category!.CategoryId);
    }

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreated()
    {
        var (eventId, categoryId) = await CreateEventWithCategoryAsync();

        var participantClient = await TestHelpers.CreateAuthenticatedClientAsync(
            _factory, $"part{Guid.NewGuid()}@test.com", "Password123!", "Participant");

        var response = await participantClient.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest(eventId, categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        var (eventId, categoryId) = await CreateEventWithCategoryAsync();

        var participantClient = await TestHelpers.CreateAuthenticatedClientAsync(
            _factory, $"part{Guid.NewGuid()}@test.com", "Password123!", "Participant");

        await participantClient.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(eventId, categoryId));
        var response = await participantClient.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(eventId, categoryId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var (eventId, categoryId) = await CreateEventWithCategoryAsync();

        var organiserClient = await TestHelpers.CreateAuthenticatedClientAsync(
            _factory, $"org2{Guid.NewGuid()}@test.com", "Password123!", "Organiser");

        var response = await organiserClient.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest(eventId, categoryId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
