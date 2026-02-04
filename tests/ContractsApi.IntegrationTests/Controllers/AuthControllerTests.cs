using ContractsApi.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Controllers;

public class AuthControllerTests : IntegrationTestFixture, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndOk()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin",
            password = "Admin@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);

        tokenResponse.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        tokenResponse.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_InvalidUsername_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            username = "wronguser",
            password = "Admin@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin",
            password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyUsername_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            username = "",
            password = "Admin@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin",
            password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenCanBeUsedForAuthorization()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin",
            password = "Admin@123"
        };

        // Act - Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await loginResponse.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
        var token = tokenResponse.GetProperty("token").GetString();

        // Act - Use token to access protected endpoint
        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        var protectedResponse = await authenticatedClient.GetAsync("/api/contratos");

        // Assert
        protectedResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_CaseSensitiveUsername_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            username = "ADMIN", // uppercase
            password = "Admin@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}