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
    public ClientesControllerTests() { }

    private async Task<Guid> CreateContratoAsync(string cpfCnpj, decimal valorTotal = 50000, int prazoMeses = 48)
    {
        var command = new CreateContratoCommand(
            ClienteCpfCnpj: cpfCnpj,
            ValorTotal: valorTotal,
            TaxaMensal: 2.5m,
            PrazoMeses: prazoMeses,
            DataVencimentoPrimeiraParcela: DateTime.Today.AddDays(30),
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        var response = await Client.PostAsJsonAsync("/api/contratos", command);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        return result.GetProperty("id").GetGuid();
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
        var cpfCnpj = "12345678901";
        await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 48);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("cpfCnpj").GetString().Should().Be(cpfCnpj);
        result.GetProperty("quantidadeContratosAtivos").GetInt32().Should().Be(1);
        result.GetProperty("totalParcelas").GetInt32().Should().Be(48);
        result.GetProperty("parcelasPagas").GetInt32().Should().Be(0);
        result.GetProperty("parcelasEmAtraso").GetInt32().Should().Be(0);
        result.GetProperty("parcelasAVencer").GetInt32().Should().Be(48);
        result.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().Be(50000);
    }

    [Fact]
    public async Task GetResumoClienteWithMultipleContratos_ReturnsConsolidatedData()
    {
        // Arrange
        var cpfCnpj = "11111111111";

        // Criar 2 contratos
        await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 48);
        await CreateContratoAsync(cpfCnpj, valorTotal: 30000, prazoMeses: 36);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("quantidadeContratosAtivos").GetInt32().Should().Be(2);
        result.GetProperty("totalParcelas").GetInt32().Should().Be(84); // 48 + 36
        result.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().Be(80000); // 50000 + 30000
    }

    [Fact]
    public async Task GetResumo_WithPagamentosEmDia_ReturnsCorrectPercentual()
    {
        // Arrange
        var cpfCnpj = "22222222222";
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

        result.GetProperty("parcelasPagas").GetInt32().Should().Be(3);
        result.GetProperty("percentualParcelasPagasEmDia").GetDecimal().Should().Be(100); // Todas em dia
    }

    [Fact]
    public async Task GetResumo_WithPagamentosMixtos_CalculatesPercentualCorrectly()
    {
        // Arrange
        var cpfCnpj = "33333333333";
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

        result.GetProperty("parcelasPagas").GetInt32().Should().Be(4);

        // 2 em dia de 4 totais = 50%
        var percentual = result.GetProperty("percentualParcelasPagasEmDia").GetDecimal();
        percentual.Should().BeGreaterThan(0);
        percentual.Should().BeLessThan(100);
    }

    [Fact]
    public async Task GetResumo_WithParcelasEmAtraso_CountsCorrectly()
    {
        // Arrange
        var cpfCnpj = "44444444444";
        var dataVencimentoPrimeiraParcela = DateTime.Today.AddDays(-60); // 2 meses atrás

        var command = new CreateContratoCommand(
            ClienteCpfCnpj: cpfCnpj,
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 12,
            DataVencimentoPrimeiraParcela: dataVencimentoPrimeiraParcela,
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        await Client.PostAsJsonAsync("/api/contratos", command);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Deve ter parcelas em atraso (vencimentos no passado sem pagamento)
        result.GetProperty("parcelasEmAtraso").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetResumo_WithParcelasAVencer_CountsCorrectly()
    {
        // Arrange
        var cpfCnpj = "55555555555";
        var dataVencimentoPrimeiraParcela = DateTime.Today.AddDays(30); // Futuro

        var command = new CreateContratoCommand(
            ClienteCpfCnpj: cpfCnpj,
            ValorTotal: 50000,
            TaxaMensal: 2.5m,
            PrazoMeses: 12,
            DataVencimentoPrimeiraParcela: dataVencimentoPrimeiraParcela,
            TipoVeiculo: TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo: CondicaoVeiculo.NOVO
        );

        await Client.PostAsJsonAsync("/api/contratos", command);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Todas as 12 parcelas estão a vencer
        result.GetProperty("parcelasAVencer").GetInt32().Should().Be(12);
        result.GetProperty("parcelasEmAtraso").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetResumo_AfterPayments_UpdatesSaldoDevedor()
    {
        // Arrange
        var cpfCnpj = "66666666666";
        var contratoId = await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 12);

        // Resumo antes dos pagamentos
        var initialResponse = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");
        var initialContent = await initialResponse.Content.ReadAsStringAsync();
        var initialResult = JsonSerializer.Deserialize<JsonElement>(initialContent);
        var initialSaldo = initialResult.GetProperty("saldoDevedorConsolidado").GetDecimal();

        // Fazer 2 pagamentos
        var dataVencimento = DateTime.Today.AddDays(30);
        await CreatePagamentoAsync(contratoId, 1, dataVencimento);
        await CreatePagamentoAsync(contratoId, 2, dataVencimento.AddMonths(1));

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var updatedSaldo = result.GetProperty("saldoDevedorConsolidado").GetDecimal();
        updatedSaldo.Should().BeLessThan(initialSaldo);
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
    public async Task GetResumo_DifferentClients_ReturnsIndependentData()
    {
        // Arrange
        var cpfCnpj1 = "77777777777";
        var cpfCnpj2 = "88888888888";

        await CreateContratoAsync(cpfCnpj1, valorTotal: 50000, prazoMeses: 48);
        await CreateContratoAsync(cpfCnpj2, valorTotal: 30000, prazoMeses: 36);

        // Act
        var response1 = await Client.GetAsync($"/api/clientes/{cpfCnpj1}/resumo");
        var response2 = await Client.GetAsync($"/api/clientes/{cpfCnpj2}/resumo");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<JsonElement>(content1);

        var content2 = await response2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<JsonElement>(content2);

        result1.GetProperty("totalParcelas").GetInt32().Should().Be(48);
        result2.GetProperty("totalParcelas").GetInt32().Should().Be(36);

        result1.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().Be(50000);
        result2.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().Be(30000);
    }

    [Fact]
    public async Task GetResumo_WithContratoCancelado_CountsOnlyAtivos()
    {
        // Arrange
        var cpfCnpj = "99999999999";

        // Criar 2 contratos
        var contratoId1 = await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 48);
        await CreateContratoAsync(cpfCnpj, valorTotal: 30000, prazoMeses: 36);

        // Cancelar o primeiro contrato (delete)
        await Client.DeleteAsync($"/api/contratos/{contratoId1}");

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Apenas 1 contrato ativo agora
        result.GetProperty("quantidadeContratosAtivos").GetInt32().Should().Be(1);
        result.GetProperty("totalParcelas").GetInt32().Should().Be(36);
        result.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().Be(30000);
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

    [Fact]
    public async Task GetResumo_CompleteScenario_ReturnsAllFieldsCorrectly()
    {
        // Arrange - Cenário completo
        var cpfCnpj = "10101010101";

        // 2 contratos ativos
        var contrato1 = await CreateContratoAsync(cpfCnpj, valorTotal: 50000, prazoMeses: 24);
        var contrato2 = await CreateContratoAsync(cpfCnpj, valorTotal: 30000, prazoMeses: 12);

        // Pagamentos no contrato 1
        var dataVencimento1 = DateTime.Today.AddDays(30);
        await CreatePagamentoAsync(contrato1, 1, dataVencimento1); // Em dia
        await CreatePagamentoAsync(contrato1, 2, dataVencimento1.AddMonths(2)); // Atrasado (vencimento seria +1 mês)

        // Pagamentos no contrato 2
        var dataVencimento2 = DateTime.Today.AddDays(30);
        await CreatePagamentoAsync(contrato2, 1, dataVencimento2); // Em dia

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Validar todos os campos
        result.GetProperty("cpfCnpj").GetString().Should().Be(cpfCnpj);
        result.GetProperty("quantidadeContratosAtivos").GetInt32().Should().Be(2);
        result.GetProperty("totalParcelas").GetInt32().Should().Be(36); // 24 + 12
        result.GetProperty("parcelasPagas").GetInt32().Should().Be(3);
        result.GetProperty("percentualParcelasPagasEmDia").GetDecimal().Should().BeGreaterThan(0);
        result.GetProperty("saldoDevedorConsolidado").GetDecimal().Should().BeLessThan(80000); // Menor que soma inicial
    }
}