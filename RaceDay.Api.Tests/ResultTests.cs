using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

public class ResultTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ResultTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AddResult_AsEventOwner_ReturnsCreated()
    {
        var organiser = _factory.CreateClient();
        await organiser.RegisterAndLogin($"owner-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await organiser.CreateEventAsOrganiser();
        var categoryId = await organiser.CreateCategoryAsOrganiser(eventId);

        var participant = _factory.CreateClient();
        await participant.RegisterAndLogin($"runner-{Guid.NewGuid():N}@example.com", "Password123!", "Participant");
        var enrolment = await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentDto
        {
            EventId = eventId,
            CategoryId = categoryId
        }, TestHelpers.Json);

        enrolment.EnsureSuccessStatusCode();
        var enrolmentDto = await enrolment.Content.ReadFromJsonAsync<EnrolmentDto>(TestHelpers.Json);

        var response = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentDto!.EnrolmentId}/result",
            new CreateResultDto { FinishTime = TimeSpan.FromMinutes(42), Position = 1 },
            TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddResult_AsParticipant_ReturnsForbidden()
    {
        var organiser = _factory.CreateClient();
        await organiser.RegisterAndLogin($"owner-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await organiser.CreateEventAsOrganiser();
        var categoryId = await organiser.CreateCategoryAsOrganiser(eventId);

        var participant = _factory.CreateClient();
        await participant.RegisterAndLogin($"runner-{Guid.NewGuid():N}@example.com", "Password123!", "Participant");
        var enrolment = await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentDto
        {
            EventId = eventId,
            CategoryId = categoryId
        }, TestHelpers.Json);

        enrolment.EnsureSuccessStatusCode();
        var enrolmentDto = await enrolment.Content.ReadFromJsonAsync<EnrolmentDto>(TestHelpers.Json);

        var response = await participant.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentDto!.EnrolmentId}/result",
            new CreateResultDto { FinishTime = TimeSpan.FromMinutes(42), Position = 1 },
            TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsParticipant_ReturnsOwnResult()
    {
        var organiser = _factory.CreateClient();
        await organiser.RegisterAndLogin($"owner-{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");
        var eventId = await organiser.CreateEventAsOrganiser();
        var categoryId = await organiser.CreateCategoryAsOrganiser(eventId);

        var participant = _factory.CreateClient();
        await participant.RegisterAndLogin($"runner-{Guid.NewGuid():N}@example.com", "Password123!", "Participant");
        var enrolment = await participant.PostAsJsonAsync("/api/enrolments", new CreateEnrolmentDto
        {
            EventId = eventId,
            CategoryId = categoryId
        }, TestHelpers.Json);

        enrolment.EnsureSuccessStatusCode();
        var enrolmentDto = await enrolment.Content.ReadFromJsonAsync<EnrolmentDto>(TestHelpers.Json);
        await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentDto!.EnrolmentId}/result",
            new CreateResultDto { FinishTime = TimeSpan.FromMinutes(40), Position = 2 },
            TestHelpers.Json);

        var response = await participant.GetAsync("/api/results/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<ResultDto>>(TestHelpers.Json);
        Assert.Single(results!);
    }
}
