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

    public ContratosFinanciamentoControllerTests() { }


    [Fact]
    public async Task Create_ValidContrato_ReturnsCreated()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("id").GetGuid().Should().NotBeEmpty();
        result.GetProperty("clienteCpfCnpj").GetString().Should().Be("12345678901");
        result.GetProperty("valorTotal").GetDecimal().Should().Be(50000);
        result.GetProperty("valorParcela").GetDecimal().Should().BeGreaterThan(0);
        result.GetProperty("saldoDevedor").GetDecimal().Should().Be(50000);
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
            CondicaoVeiculo: CondicaoVeiculo.NOVO
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
            CondicaoVeiculo: CondicaoVeiculo.NOVO
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
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/contratos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAll_AfterCreatingContrato_ReturnsListWithOneItem()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        await Client.PostAsJsonAsync("/api/contratos", command);

        // Act
        var response = await Client.GetAsync("/api/contratos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetById_ExistingContrato_ReturnsOk()
    {
        // Arrange
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        var createResponse = await Client.PostAsJsonAsync("/api/contratos", command);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var contratoId = createResult.GetProperty("id").GetGuid();

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("id").GetGuid().Should().Be(contratoId);
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
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        var createResponse = await Client.PostAsJsonAsync("/api/contratos", command);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var contratoId = createResult.GetProperty("id").GetGuid();

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
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        var createResponse = await Client.PostAsJsonAsync("/api/contratos", command);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var contratoId = createResult.GetProperty("id").GetGuid();

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
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/contratos", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}