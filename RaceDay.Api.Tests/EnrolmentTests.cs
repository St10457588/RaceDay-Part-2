using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

public class EnrolmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EnrolmentTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task EnrolParticipant_InEventAndCategory_ReturnsCreated()
    {
        var organiser = _factory.CreateClient();
        await organiser.RegisterAndLogin($"organiser-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await organiser.CreateEventAsOrganiser();
        var categoryId = await organiser.CreateCategoryAsOrganiser(eventId);

        var participant = _factory.CreateClient();
        await participant.RegisterAndLogin($"participant-{Guid.NewGuid():N}@example.com", "Password123!", "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentDto
        {
            EventId = eventId,
            CategoryId = categoryId
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task EnrolParticipant_TwiceInSameEvent_ReturnsConflict()
    {
        var organiser = _factory.CreateClient();
        await organiser.RegisterAndLogin($"organiser-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await organiser.CreateEventAsOrganiser();
        var categoryId = await organiser.CreateCategoryAsOrganiser(eventId);

        var participant = _factory.CreateClient();
        await participant.RegisterAndLogin($"participant-{Guid.NewGuid():N}@example.com", "Password123!", "Participant");

        var enrolment = new CreateEnrolmentDto { EventId = eventId, CategoryId = categoryId };
        var first = await participant.PostAsJsonAsync("/api/enrolments", enrolment, TestHelpers.Json);
        var second = await participant.PostAsJsonAsync("/api/enrolments", enrolment, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var organiser = _factory.CreateClient();
        await organiser.RegisterAndLogin($"organiser-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await organiser.CreateEventAsOrganiser();
        var categoryId = await organiser.CreateCategoryAsOrganiser(eventId);

        var response = await organiser.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentDto
        {
            EventId = eventId,
            CategoryId = categoryId
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEventEnrolments_AsOwner_ReturnsEnrolments()
    {
        var organiser = _factory.CreateClient();
        await organiser.RegisterAndLogin($"owner-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await organiser.CreateEventAsOrganiser();
        var categoryId = await organiser.CreateCategoryAsOrganiser(eventId);

        var participant = _factory.CreateClient();
        await participant.RegisterAndLogin($"participant-{Guid.NewGuid():N}@example.com", "Password123!", "Participant");
        await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentDto
        {
            EventId = eventId,
            CategoryId = categoryId
        }, TestHelpers.Json);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>(TestHelpers.Json);
        Assert.Single(enrolments!);
    }
}
