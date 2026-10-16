using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

public static class TestHelpers
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<HttpResponseMessage> RegisterAndLogin(
        this HttpClient client, string email, string password, string role)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = role == "Organiser" ? "Test Organiser" : "Test Participant",
            Email = email,
            Password = password,
            Role = role == "Organiser" ? UserRole.Organiser : UserRole.Participant
        }, Json);

        return await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        }, Json);
    }

    public static async Task<int> CreateEventAsOrganiser(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/events", new CreateEventDto
        {
            Name = "Durban 10K",
            Description = "A scenic 10km road run.",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Durban",
            DistanceKm = 10,
            EventType = EventType.Run
        }, Json);

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return created.GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategoryAsOrganiser(this HttpClient client, int eventId)
    {
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryDto
        {
            Name = "Senior",
            MinAge = 20,
            MaxAge = 59,
            DistanceKm = 10
        }, Json);

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return created.GetProperty("categoryId").GetInt32();
    }
}
