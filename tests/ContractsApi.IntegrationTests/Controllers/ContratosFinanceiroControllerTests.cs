using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Enums;
using ContractsApi.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Controllers;

public class ContratosFinanciamentoControllerTests : IntegrationTestFixture
{

    public ContratosFinanciamentoControllerTests() : base() { }


    [Fact]
    public async Task Create_ValidContrato_ReturnsCreated()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "35711699059",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        data.GetProperty("id").GetGuid().Should().NotBeEmpty();
        data.GetProperty("clienteCpfCnpj").GetString().Should().Be("35711699059");
        data.GetProperty("valorTotal").GetDecimal().Should().Be(50000);
        data.GetProperty("valorParcela").GetDecimal().Should().BeGreaterThan(0);
        data.GetProperty("saldoDevedor").GetDecimal().Should().Be(50000);
    }

    [Fact]
    public async Task Create_InvalidCpf_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "123", // CPF inválido
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NegativeValorTotal_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "12345678901",
            ValorTotal: -1000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_PastDataVencimento_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(-1), // Data no passado
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ExistingContrato_ReturnsOk()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "65973205061",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        var createResponse = await Client.PostAsJsonAsync("/api/contratos", command);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var createdData = createResult.GetProperty("data");
        var contratoId = createdData.GetProperty("id").GetGuid();

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        data.GetProperty("id").GetGuid().Should().Be(contratoId);
    }

    [Fact]
    public async Task GetById_NonExistingContrato_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/contratos/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingContrato_ReturnsNoContent()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "23894066024",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        var createResponse = await Client.PostAsJsonAsync("/api/contratos", command);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var createdData = createResult.GetProperty("data");
        var contratoId = createdData.GetProperty("id").GetGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/contratos/{contratoId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistingContrato_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/contratos/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "07349756003",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        var createResponse = await Client.PostAsJsonAsync("/api/contratos", command);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var createdData = createResult.GetProperty("data");
        var contratoId = createdData.GetProperty("id").GetGuid();

        // Act
        await Client.DeleteAsync($"/api/contratos/{contratoId}");
        var getResponse = await Client.GetAsync($"/api/contratos/{contratoId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = Factory.CreateClient();

        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}