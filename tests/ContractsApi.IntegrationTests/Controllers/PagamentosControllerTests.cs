using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Enums;
using ContractsApi.IntegrationTests.Fixtures;
using ContractsApi.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Controllers;

public class PagamentosControllerTests : IntegrationTestFixture
{
    public PagamentosControllerTests() : base() { }

    [Fact]
    public async Task Create_ValidPagamento_ReturnsCreated()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        var result = await ResponseValidationHelper.ValidateCreatedResponseAsync(response);
        var data = result.GetData();

        PagamentoTestHelper.ValidatePagamentoResponse(data, contratoId, 1);
    }

    [Fact]
    public async Task Create_PagamentoEmDia_ReturnsStatusEmDia()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var dataVencimento = DateTime.Today.AddDays(30);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest(
            numeroParcela: 1,
            dataPagamento: dataVencimento
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        var result = await ResponseValidationHelper.ValidateCreatedResponseAsync(response);
        var data = result.GetData();

        PagamentoTestHelper.ValidatePagamentoStatus(data, "EM_DIA");
    }

    [Fact]
    public async Task Create_PagamentoAntecipado_ReturnsStatusAntecipado()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest(
            numeroParcela: 1,
            dataPagamento: DateTime.Today.AddDays(20)
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        var result = await ResponseValidationHelper.ValidateCreatedResponseAsync(response);
        var data = result.GetData();

        PagamentoTestHelper.ValidatePagamentoStatus(data, "ANTECIPADO");
    }

    [Fact]
    public async Task Create_PagamentoAtrasado_ReturnsStatusEmAtraso()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest(
            numeroParcela: 1,
            dataPagamento: DateTime.Today.AddDays(40)
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        var result = await ResponseValidationHelper.ValidateCreatedResponseAsync(response);
        var data = result.GetData();

        PagamentoTestHelper.ValidatePagamentoStatus(data, "EM_ATRASO");
    }

    [Fact]
    public async Task Create_ContratoNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistingContratoId = Guid.NewGuid();
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{nonExistingContratoId}/pagamentos", pagamentoRequest);

        // Assert
        ResponseValidationHelper.ValidateNotFoundResponse(response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidNumeroParcela_ReturnsBadRequest()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest(numeroParcela: 0);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        ResponseValidationHelper.ValidateBadRequestResponse(response.StatusCode);
    }

    [Fact]
    public async Task Create_NegativeValorPago_ReturnsBadRequest()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest(valorPago: -100m);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        ResponseValidationHelper.ValidateBadRequestResponse(response.StatusCode);
    }

    [Fact]
    public async Task Create_ParcelaExceedsPrazo_ReturnsBadRequest()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest(numeroParcela: 100);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        ResponseValidationHelper.ValidateBadRequestResponse(response.StatusCode);
    }

    [Fact]
    public async Task GetByContrato_EmptyList_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/contratos/{Guid.NewGuid()}/pagamentos");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetDataArray();

        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetByContrato_AfterCreatingPagamento_ReturnsListWithOneItem()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest();

        await Client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}/pagamentos");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetDataArray();

        data.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetByContrato_MultiplePagamentos_ReturnsOrderedList()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var dataVencimentoPrimeiraParcela = DateTime.Today.AddDays(30);

        await PagamentoTestHelper.CreateMultiplePagamentosAsync(
            Client,
            contratoId,
            quantidadeParcelas: 3,
            dataVencimentoPrimeiraParcela
        );

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}/pagamentos");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetDataArray();

        data.GetArrayLength().Should().Be(3);

        // Verificar ordem
        var firstParcela = data[0].GetIntValue("numeroParcela");
        var secondParcela = data[1].GetIntValue("numeroParcela");
        var thirdParcela = data[2].GetIntValue("numeroParcela");

        firstParcela.Should().BeLessThan(secondParcela);
        secondParcela.Should().BeLessThan(thirdParcela);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = Factory.CreateClient();
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client);
        var pagamentoRequest = PagamentoTestHelper.CreateDefaultPagamentoRequest();

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", pagamentoRequest);

        // Assert
        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
    }
}