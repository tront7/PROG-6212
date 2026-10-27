using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Tests;

/// <summary>Tests for the API features added for the MVC front end: event type/distance,
/// filtering, the organiser dashboard feed, Azure Blob image uploads, enrolment status and results.</summary>
public class Part3ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Part3ApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpClient> NewOrganiserAsync() =>
        TestHelpers.CreateAuthenticatedClientAsync(_factory, $"org{Guid.NewGuid()}@test.com", "Password123!", "Organiser", "Olivia Organiser");

    private Task<HttpClient> NewParticipantAsync() =>
        TestHelpers.CreateAuthenticatedClientAsync(_factory, $"par{Guid.NewGuid()}@test.com", "Password123!", "Participant", "Pat Participant");

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_ReturnsBadRequest()
    {
        var organiser = await NewOrganiserAsync();

        var response = await organiser.PostAsJsonAsync("/api/events",
            new CreateEventRequest("Swim Gala", DateTime.UtcNow.AddMonths(1), "Cape Town", "desc", 5, "Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_FilteredByType_ReturnsOnlyThatType()
    {
        var organiser = await NewOrganiserAsync();
        var name = $"Cycle-{Guid.NewGuid():N}";
        await TestHelpers.CreateEventWithCategoryAsync(organiser, name, "Cycle");
        await TestHelpers.CreateEventWithCategoryAsync(organiser, $"Run-{Guid.NewGuid():N}", "Run");

        var response = await _factory.CreateClient().GetAsync("/api/events?eventType=Cycle");
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(events);
        Assert.NotEmpty(events!);
        Assert.All(events!, e => Assert.Equal("Cycle", e.EventType));
        Assert.Contains(events!, e => e.EventName == name);
    }

    [Fact]
    public async Task GetMyEvents_AsOrganiser_ReturnsOwnEventsWithEnrolmentCounts()
    {
        var organiser = await NewOrganiserAsync();
        var name = $"Mine-{Guid.NewGuid():N}";
        await TestHelpers.CreateEventWithCategoryAsync(organiser, name);

        var response = await organiser.GetAsync("/api/events/mine");
        var events = await response.Content.ReadFromJsonAsync<List<EventSummaryResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = Assert.Single(events!);
        Assert.Equal(name, mine.EventName);
        Assert.Equal(0, mine.TotalEnrolments);
        Assert.Equal(1, mine.CategoryCount);
        Assert.True(mine.IsUpcoming);
    }

    [Fact]
    public async Task GetMyEvents_AsParticipant_ReturnsForbidden()
    {
        var participant = await NewParticipantAsync();

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsOrganiser_StoresAndReturnsBlobUrl()
    {
        var organiser = await NewOrganiserAsync();
        var (ev, _) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var upload = await organiser.PostAsync($"/api/events/{ev.EventId}/banner",
            TestHelpers.FileUpload("banner.png", "image/png"));
        var body = await upload.Content.ReadFromJsonAsync<ImageUploadResponse>();

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.StartsWith("https://fake-blob.test/event-banners/", body!.Url);

        // The URL is saved against the event so the MVC app can display it.
        var fetched = await _factory.CreateClient().GetFromJsonAsync<EventResponse>($"/api/events/{ev.EventId}");
        Assert.Equal(body.Url, fetched!.BannerImageUrl);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageFile_ReturnsBadRequest()
    {
        var organiser = await NewOrganiserAsync();
        var (ev, _) = await TestHelpers.CreateEventWithCategoryAsync(organiser);

        var upload = await organiser.PostAsync($"/api/events/{ev.EventId}/banner",
            TestHelpers.FileUpload("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_ReturnsForbidden()
    {
        var participant = await NewParticipantAsync();

        var upload = await participant.PostAsync("/api/events/1/banner",
            TestHelpers.FileUpload("banner.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_AsLoggedInUser_ReturnsUrlAndUpdatesProfile()
    {
        var participant = await NewParticipantAsync();

        var upload = await participant.PostAsync("/api/users/me/profile-picture",
            TestHelpers.FileUpload("me.jpg", "image/jpeg"));
        var body = await upload.Content.ReadFromJsonAsync<ImageUploadResponse>();

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var profile = await participant.GetFromJsonAsync<UserProfileResponse>("/api/users/me");
        Assert.Equal(body!.Url, profile!.ProfilePictureUrl);
    }

    [Fact]
    public async Task OrganiserCanUpdateEnrolmentStatus_AndParticipantSeesIt()
    {
        var organiser = await NewOrganiserAsync();
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var participant = await NewParticipantAsync();

        var enrol = await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(ev.EventId, category.CategoryId));
        var enrolment = await enrol.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);
        Assert.Equal("Confirmed", enrolment!.Status);

        var update = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest("Pending"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/me");
        Assert.Equal("Pending", Assert.Single(mine!).Status);
    }

    [Fact]
    public async Task ParticipantCannotUpdateEnrolmentStatus()
    {
        var participant = await NewParticipantAsync();

        var update = await participant.PutAsJsonAsync("/api/enrolments/1/status", new UpdateEnrolmentStatusRequest("Pending"));

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task CapturedResult_AppearsInParticipantResults_WithPositionAndFinisherCount()
    {
        var organiser = await NewOrganiserAsync();
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser, $"Results-{Guid.NewGuid():N}");
        var participant = await NewParticipantAsync();

        var enrol = await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(ev.EventId, category.CategoryId));
        var enrolment = await enrol.Content.ReadFromJsonAsync<EnrolmentResponse>();

        var capture = await organiser.PostAsJsonAsync("/api/results",
            new CreateResultRequest(enrolment!.EnrolmentId, new TimeSpan(0, 52, 30), 1, "Finished"));
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var results = await participant.GetFromJsonAsync<List<MyResultResponse>>("/api/results/me");
        var result = Assert.Single(results!);
        Assert.Equal(1, result.Position);
        Assert.Equal(1, result.TotalFinishers);
        Assert.Equal(new TimeSpan(0, 52, 30), result.FinishTime);
        Assert.Equal(ev.EventName, result.EventName);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_ReturnsForbidden()
    {
        var participant = await NewParticipantAsync();

        var response = await participant.PostAsJsonAsync("/api/results",
            new CreateResultRequest(1, new TimeSpan(0, 40, 0), 1, "Finished"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_ReturnsConflict()
    {
        var organiser = await NewOrganiserAsync();
        var (ev, category) = await TestHelpers.CreateEventWithCategoryAsync(organiser);
        var participant = await NewParticipantAsync();
        await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentRequest(ev.EventId, category.CategoryId));

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }
}
