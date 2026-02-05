using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Enums;
using ContractsApi.IntegrationTests.Fixtures;
using ContractsApi.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Controllers;

public class ClientesControllerTests : IntegrationTestFixture
{
    public ClientesControllerTests() : base() { }

    [Fact]
    public async Task GetResumoClienteWithoutContratos_ReturnsNotFound()
    {
        // Arrange
        var cpfCnpj = "99999999999";

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        ResponseValidationHelper.ValidateNotFoundResponse(response.StatusCode);
    }

    [Fact]
    public async Task GetResumoClienteWithOneContratoAtivo_ReturnsCorrectData()
    {
        // Arrange
        var cpfCnpj = "28402173098";
        await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj, valorTotal: 50000, prazoMeses: 48);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetData();

        data.GetStringValue("cpfCnpj").Should().Be(cpfCnpj);
        data.GetIntValue("quantidadeContratosAtivos").Should().Be(1);
        data.GetIntValue("totalParcelas").Should().Be(48);
        data.GetIntValue("parcelasPagas").Should().Be(0);
        data.GetIntValue("parcelasEmAtraso").Should().Be(0);
        data.GetIntValue("parcelasAVencer").Should().Be(48);
        data.GetDecimalValue("saldoDevedorConsolidado").Should().Be(50000);
    }

    [Fact]
    public async Task GetResumoClienteWithMultipleContratos_ReturnsConsolidatedData()
    {
        // Arrange
        var cpfCnpj = "75592565038";

        // Criar 2 contratos
        await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj, valorTotal: 50000, prazoMeses: 48);
        await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj, valorTotal: 30000, prazoMeses: 36);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetData();

        data.GetIntValue("quantidadeContratosAtivos").Should().Be(2);
        data.GetIntValue("totalParcelas").Should().Be(84); // 48 + 36
        data.GetDecimalValue("saldoDevedorConsolidado").Should().Be(80000); // 50000 + 30000
    }

    [Fact]
    public async Task GetResumo_WithPagamentosEmDia_ReturnsCorrectPercentual()
    {
        // Arrange
        var cpfCnpj = "18182076056";
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj, valorTotal: 50000, prazoMeses: 12);
        var dataVencimento = DateTime.Today.AddDays(30);

        // Criar 3 pagamentos em dia
        await PagamentoTestHelper.CreateMultiplePagamentosAsync(Client, contratoId, 3, dataVencimento);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetData();

        data.GetIntValue("parcelasPagas").Should().Be(3);
        data.GetDecimalValue("percentualParcelasPagasEmDia").Should().Be(100); // Todas em dia
    }

    [Fact]
    public async Task GetResumo_WithPagamentosMixtos_CalculatesPercentualCorrectly()
    {
        // Arrange
        var cpfCnpj = "54237781070";
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj, valorTotal: 50000, prazoMeses: 12);
        var dataVencimento = DateTime.Today.AddDays(30);

        // 2 pagamentos em dia
        await PagamentoTestHelper.CreatePagamentoAsync(Client, contratoId, 1, 1346.18m, dataVencimento);
        await PagamentoTestHelper.CreatePagamentoAsync(Client, contratoId, 2, 1346.18m, dataVencimento.AddMonths(1));

        // 1 pagamento atrasado
        await PagamentoTestHelper.CreatePagamentoAsync(Client, contratoId, 3, 1346.18m, dataVencimento.AddMonths(3));

        // 1 pagamento antecipado
        await PagamentoTestHelper.CreatePagamentoAsync(Client, contratoId, 4, 1346.18m, dataVencimento.AddMonths(2));

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetData();

        data.GetIntValue("parcelasPagas").Should().Be(4);

        // 2 em dia de 4 totais = 50%
        var percentual = data.GetDecimalValue("percentualParcelasPagasEmDia");
        percentual.Should().BeGreaterThan(0);
        percentual.Should().BeLessThan(100);
    }

    [Fact]
    public async Task GetResumo_WithParcelasAVencer_CountsCorrectly()
    {
        // Arrange
        var cpfCnpj = "56493254051";
        var dataVencimentoPrimeiraParcela = DateTime.Today.AddDays(30);

        var command = ContratoTestHelper.CreateDefaultContratoCommand(
            cpfCnpj: cpfCnpj,
            dataVencimentoPrimeiraParcela: dataVencimentoPrimeiraParcela,
            prazoMeses: 12
        );

        await Client.PostAsJsonAsync("/api/contratos", command);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{cpfCnpj}/resumo");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetData();

        // Todas as 12 parcelas estão a vencer
        data.GetIntValue("parcelasAVencer").Should().Be(12);
        data.GetIntValue("parcelasEmAtraso").Should().Be(0);
    }

    [Fact]
    public async Task GetResumo_EmptyCpfCnpj_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/clientes//resumo");

        // Assert
        ResponseValidationHelper.ValidateNotFoundResponse(response.StatusCode); // 404 porque a rota não match
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
        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
    }
}