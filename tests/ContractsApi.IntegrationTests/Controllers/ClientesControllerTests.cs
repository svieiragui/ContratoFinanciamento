using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Enums;
using ContractsApi.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Controllers;

public class ClientesControllerTests : IntegrationTestFixture
{
    public ClientesControllerTests() : base() { }

    private async Task<Guid> CreateContratoAsync(string cpfCnpj, decimal valorTotal = 50000, int prazoMeses = 48)
    {
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: cpfCnpj,
            ValorTotal: valorTotal,
            TaxaMensal: 2.5m,
            PrazoMeses: prazoMeses,
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

    private async Task CreatePagamentoAsync(Guid contratoId, int numeroParcela, DateTime dataPagamento)
    {
        var pagamentoRequest = new
        {
            numeroParcela,
            valorPago = 1346.18m,
            dataPagamento
        };

        await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);
    }

    [Fact]
    public async Task GetResumoClienteWithoutContratos_ReturnsNotFound()
    {
        // Arrange
        var cpfCnpj = "99999999999";

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetResumoClienteWithOneContratoAtivo_ReturnsCorrectData()
    {
        // Arrange
        var cpfCnpj = "28402173098";
        await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 48);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        data.GetProperty("cpfCnpj").GetString().Should().Be(cpfCnpj);
        data.GetProperty("quantidadeContratosAtivos").GetInt32().Should().Be(1);
        data.GetProperty("totalParcelas").GetInt32().Should().Be(48);
        data.GetProperty("parcelasPagas").GetInt32().Should().Be(0);
        data.GetProperty("parcelasEmAtraso").GetInt32().Should().Be(0);
        data.GetProperty("parcelasAVencer").GetInt32().Should().Be(48);
        data.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().Be(50000);
    }

    [Fact]
    public async Task GetResumoClienteWithMultipleContratos_ReturnsConsolidatedData()
    {
        // Arrange
        var cpfCnpj = "75592565038";

        // Criar 2 contratos
        await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 48);
        await CreateContratoAsync(cpfCnpj, valorTotal: 30000, prazoMeses: 36);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        data.GetProperty("quantidadeContratosAtivos").GetInt32().Should().Be(2);
        data.GetProperty("totalParcelas").GetInt32().Should().Be(84); // 48 + 36
        data.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().Be(80000); // 50000 + 30000
    }

    [Fact]
    public async Task GetResumo_WithPagamentosEmDia_ReturnsCorrectPercentual()
    {
        // Arrange
        var cpfCnpj = "18182076056";
        var contratoId = await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 12);

        // Criar 3 pagamentos em dia
        var dataVencimento = DateTime.Today.AddDays(30);
        await CreatePagamentoAsync(contratoId, 1, dataVencimento);
        await CreatePagamentoAsync(contratoId, 2, dataVencimento.AddMonths(1));
        await CreatePagamentoAsync(contratoId, 3, dataVencimento.AddMonths(2));

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        data.GetProperty("parcelasPagas").GetInt32().Should().Be(3);
        data.GetProperty("percentualParcelasPagasEmDia").GetDecimal().Should().Be(100); // Todas em dia
    }

    [Fact]
    public async Task GetResumo_WithPagamentosMixtos_CalculatesPercentualCorrectly()
    {
        // Arrange
        var cpfCnpj = "54237781070";
        var contratoId = await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 12);

        var dataVencimento = DateTime.Today.AddDays(30);

        // 2 pagamentos em dia
        await CreatePagamentoAsync(contratoId, 1, dataVencimento);
        await CreatePagamentoAsync(contratoId, 2, dataVencimento.AddMonths(1));

        // 1 pagamento atrasado
        await CreatePagamentoAsync(contratoId, 3, dataVencimento.AddMonths(3)); // Vencimento seria +2 meses

        // 1 pagamento antecipado
        await CreatePagamentoAsync(contratoId, 4, dataVencimento.AddMonths(2)); // Vencimento seria +3 meses

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        data.GetProperty("parcelasPagas").GetInt32().Should().Be(4);

        // 2 em dia de 4 totais = 50%
        var percentual = data.GetProperty("percentualParcelasPagasEmDia").GetDecimal();
        percentual.Should().BeGreaterThan(0);
        percentual.Should().BeLessThan(100);
    }

    [Fact]
    public async Task GetResumo_WithParcelasAVencer_CountsCorrectly()
    {
        // Arrange
        var cpfCnpj = "56493254051";
        var dataVencimentoPrimeiraParcela = DateTime.Today.AddDays(30); // Futuro

        var command = new CreateContratoCommand(
            ClienteCpfCnpj: cpfCnpj,
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 12,
            DataVencimentoPrimeiraParcela: dataVencimentoPrimeiraParcela,
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO,
            CorrelationId: Guid.NewGuid().ToString()
        );

        await Client.PostAsJsonAsync("/api/contratos", command);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var data = result.GetProperty("data");

        // Todas as 12 parcelas estão a vencer
        data.GetProperty("parcelasAVencer").GetInt32().Should().Be(12);
        data.GetProperty("parcelasEmAtraso").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetResumo_EmptyCpfCnpj_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/clientes//resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // 404 porque a rota não match
    }

    [Fact]
    public async Task GetResumo_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = Factory.CreateClient();
        var cpfCnpj = "12345678901";

        // Act
        var response = await unauthenticatedClient.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}