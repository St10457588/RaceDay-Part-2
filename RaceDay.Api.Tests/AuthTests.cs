using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidParticipantData_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Garnette Tester",
            Email = $"participant-{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            Role = UserRole.Participant
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var first = await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "First User",
            Email = email,
            Password = "Password123!",
            Role = UserRole.Participant
        }, TestHelpers.Json);

        var second = await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Second User",
            Email = email,
            Password = "Password123!",
            Role = UserRole.Participant
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Login User",
            Email = email,
            Password = "Password123!",
            Role = UserRole.Participant
        }, TestHelpers.Json);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = "Password123!"
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var email = $"badlogin-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Bad Login",
            Email = email,
            Password = "Password123!",
            Role = UserRole.Participant
        }, TestHelpers.Json);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = "WrongPassword!"
        }, TestHelpers.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
