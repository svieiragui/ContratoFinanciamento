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
    public PagamentosControllerTests() { }
    
    private async Task<Guid> CreateContratoAsync()
    {
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: "12345678901",
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 48,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        var response = await Client.PostAsJsonAsync("/api/contratos", command);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("id").GetGuid();
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

        result.GetProperty("id").GetGuid().Should().NotBeEmpty();
        result.GetProperty("contratoId").GetGuid().Should().Be(contratoId);
        result.GetProperty("numeroParcela").GetInt32().Should().Be(1);
        result.GetProperty("valorPago").GetDecimal().Should().Be(1346.18m);
        result.GetProperty("status").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("jurosPago").GetDecimal().Should().BeGreaterThan(0);
        result.GetProperty("amortizacaoPaga").GetDecimal().Should().BeGreaterThan(0);
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

        result.GetProperty("status").GetString().Should().Be("EM_DIA");
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

        result.GetProperty("status").GetString().Should().Be("ANTECIPADO");
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

        result.GetProperty("status").GetString().Should().Be("EM_ATRASO");
    }

    [Fact]
    public async Task Create_DuplicateParcela_ReturnsConflict()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today.AddDays(30)
        };

        // Primeiro pagamento
        await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Act - Segundo pagamento da mesma parcela
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
        // Arrange
        var contratoId = await CreateContratoAsync();

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}/pagamentos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

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
        var result = JsonSerializer.Deserialize<JsonElement>(content);

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
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetArrayLength().Should().Be(3);

        // Verificar ordem
        var firstParcela = result[0].GetProperty("numeroParcela").GetInt32();
        var secondParcela = result[1].GetProperty("numeroParcela").GetInt32();
        var thirdParcela = result[2].GetProperty("numeroParcela").GetInt32();

        firstParcela.Should().BeLessThan(secondParcela);
        secondParcela.Should().BeLessThan(thirdParcela);
    }

    [Fact]
    public async Task Create_UpdatesSaldoDevedor_Correctly()
    {
        // Arrange
        var contratoId = await CreateContratoAsync();

        // Get contrato inicial
        var initialContratoResponse = await Client.GetAsync($"/api/contratos/{contratoId}");
        var initialContent = await initialContratoResponse.Content.ReadAsStringAsync();
        var initialContrato = JsonSerializer.Deserialize<JsonElement>(initialContent);
        var initialSaldo = initialContrato.GetProperty("saldoDevedor").GetDecimal();

        var pagamentoRequest = new
        {
            numeroParcela = 1,
            valorPago = 1346.18m,
            dataPagamento = DateTime.Today.AddDays(30)
        };

        // Act
        var createPagamentoResponse = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);
        var pagamentoContent = await createPagamentoResponse.Content.ReadAsStringAsync();
        var pagamentoResult = JsonSerializer.Deserialize<JsonElement>(pagamentoContent);

        // Get contrato após pagamento
        var updatedContratoResponse = await Client.GetAsync($"/api/contratos/{contratoId}");
        var updatedContent = await updatedContratoResponse.Content.ReadAsStringAsync();
        var updatedContrato = JsonSerializer.Deserialize<JsonElement>(updatedContent);
        var updatedSaldo = updatedContrato.GetProperty("saldoDevedor").GetDecimal();

        // Assert
        var amortizacao = pagamentoResult.GetProperty("amortizacaoPaga").GetDecimal();
        updatedSaldo.Should().Be(initialSaldo - amortizacao);
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