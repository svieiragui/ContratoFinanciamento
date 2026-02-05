using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Enums;
using ContractsApi.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace ContractsApi.IntegrationTests.Controllers;

public class PagamentosControllerTests : IntegrationTestFixture
{
    public PagamentosControllerTests() : base() { }
    
    private async Task<Guid> CreateContratoAsync()
    {
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "13668835004",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        var response = await Client.PostAsJsonAsync("/api/contratos", command);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("data").GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Create_ValidPagamento_ReturnsCreated()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today.AddDays(30)
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        data.GetProperty("id").GetGuid().Should().NotBeEmpty();
        data.GetProperty("contratoId").GetGuid().Should().Be(contratoId);
    }

    [Fact]
    public async Task Create_PagamentoEmDia_ReturnsStatusEmDia()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();
        var dataVencimento = DateTime.Today.AddDays(30);

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = dataVencimento // Mesmo dia do vencimento
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("data").GetProperty("status").GetString().Should().Be("EM_DIA");
    }

    [Fact]
    public async Task Create_PagamentoAntecipado_ReturnsStatusAntecipado()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today.AddDays(20) // Antes do vencimento (dia 30)
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("data").GetProperty("status").GetString().Should().Be("ANTECIPADO");
    }

    [Fact]
    public async Task Create_PagamentoAtrasado_ReturnsStatusEmAtraso()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today.AddDays(40) // Depois do vencimento (dia 30)
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("data").GetProperty("status").GetString().Should().Be("EM_ATRASO");
    }

    [Fact]
    public async Task Create_ContratoNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistingContratoId = Guid.NewGuid();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{nonExistingContratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_InvalidNumeroParcela_ReturnsBadRequest()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 0, // Inválido
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NegativeValorPago_ReturnsBadRequest()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = -100m, // Negativo
            dataPagamento = DateTime.Today
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ParcelaExceedsPrazo_ReturnsBadRequest()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 100, // Contrato tem 48 parcelas
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByContrato_EmptyList_ReturnsOk()
    {
        var guid = Guid.NewGuid();
        // Act
        var response = await Client.GetAsync($"/api/contratos/{guid}/pagamentos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content).GetProperty("data");

        result.ValueKind.Should().Be(JsonValueKind.Array);
        result.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetByContrato_AfterCreatingPagamento_ReturnsListWithOneItem()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today.AddDays(30)
        };

        await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}/pagamentos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content).GetProperty("data");

        result.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetByContrato_MultiplePagamentos_ReturnsOrderedList()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        // Criar 3 pagamentos
        for (int i = 1; i <= 3; i++)
        {
            var pagamentoRequest = new
            {
                numeroParcela = i,
                valorPago = 1346.18m,
                dataPagamento = DateTime.Today.AddMonths(i)
            };

            await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);
        }

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}/pagamentos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content).GetProperty("data");

        result.GetArrayLength().Should().Be(3);

        // Verificar ordem
        var firstParcela = result[0].GetProperty("numeroParcela").GetInt32();
        var secondParcela = result[1].GetProperty("numeroParcela").GetInt32();
        var thirdParcela = result[2].GetProperty("numeroParcela").GetInt32();

        firstParcela.Should().BeLessThan(secondParcela);
        secondParcela.Should().BeLessThan(thirdParcela);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = Factory.CreateClient();
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today
        };

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}