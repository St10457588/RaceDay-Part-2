using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EventTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await client.RegisterAndLogin($"organiser-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", new CreateEventDto
        {
            Name = "Test 5K",
            Description = "Test event",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Durban",
            DistanceKm = 5,
            EventType = EventType.Run
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await client.RegisterAndLogin($"participant-{Guid.NewGuid():N}@example.com", "Password123!", "Participant");

        var response = await client.PostAsJsonAsync("/api/events", new CreateEventDto
        {
            Name = "Forbidden Event",
            Description = "Should not be allowed",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Durban",
            DistanceKm = 5,
            EventType = EventType.Run
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsOtherOrganiser_ReturnsForbidden()
    {
        var owner = _factory.CreateClient();
        await owner.RegisterAndLogin($"owner-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await owner.CreateEventAsOrganiser();

        var attacker = _factory.CreateClient();
        await attacker.RegisterAndLogin($"other-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");

        var response = await attacker.PutAsJsonAsync($"/api/events/{eventId}", new UpdateEventDto
        {
            Name = "Hacked Event",
            Description = "Should not be allowed",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Durban",
            DistanceKm = 5,
            EventType = EventType.Run
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwner_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await client.RegisterAndLogin($"delete-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await client.CreateEventAsOrganiser();

        var response = await client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
