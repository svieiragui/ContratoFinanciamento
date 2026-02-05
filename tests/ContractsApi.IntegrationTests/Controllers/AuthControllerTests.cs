using ContractsApi.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Controllers;

public class AuthControllerTests
{
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _client = new HttpClient { BaseAddress = new Uri("http://localhost") };
    }

    private object CreateLoginRequest(string username = "admin", string password = "Admin@123")
    {
        return new { username, password };
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndOk()
    {
        var loginRequest = CreateLoginRequest();
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
    }

    [Fact]
    public async Task Login_InvalidUsername_ReturnsUnauthorized()
    {
        var loginRequest = CreateLoginRequest(username: "wronguser");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var loginRequest = CreateLoginRequest(password: "WrongPassword");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyUsername_ReturnsUnauthorized()
    {
        var loginRequest = CreateLoginRequest(username: "");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsUnauthorized()
    {
        var loginRequest = CreateLoginRequest(password: "");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenCanBeUsedForAuthorization()
    {
        var loginRequest = CreateLoginRequest();
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonResponseHelper.Deserialize(content);
        var token = tokenResponse.GetStringValue("token");

        var authenticatedClient = new HttpClient { BaseAddress = new Uri("http://localhost") };
        authenticatedClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        var protectedResponse = await authenticatedClient.GetAsync("/api/contratos");

        protectedResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_CaseSensitiveUsername_ReturnsUnauthorized()
    {
        var loginRequest = CreateLoginRequest(username: "ADMIN");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
    }
}