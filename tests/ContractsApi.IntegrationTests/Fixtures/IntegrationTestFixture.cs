using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace ContractsApi.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected HttpClient UnauthenticatedClient { get; private set; } = null!;
    protected string Token { get; private set; } = string.Empty;
    protected string ConnectionString => _postgresContainer.GetConnectionString();

    public IntegrationTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("contracts_db")
            .WithUsername("contracts_user")
            .WithPassword("contracts_password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await DatabaseSeeder.SeedDatabaseAsync(ConnectionString);

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            });

        UnauthenticatedClient = Factory.CreateClient();
        Client = Factory.CreateClient();
        Token = await AuthenticateAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
    }

    private async Task<string> AuthenticateAsync()
    {
        var loginRequest = new { username = "admin", password = "Admin@123" };
        var response = await UnauthenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
        return tokenResponse.GetProperty("token").GetString()!;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        UnauthenticatedClient?.Dispose();
        Factory?.Dispose();
        await _postgresContainer.DisposeAsync();
    }
}