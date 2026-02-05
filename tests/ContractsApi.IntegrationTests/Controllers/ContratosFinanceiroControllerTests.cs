using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Enums;
using ContractsApi.IntegrationTests.Fixtures;
using ContractsApi.IntegrationTests.Helpers;
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
        var command = ContratoTestHelper.CreateDefaultContratoCommand(cpfCnpj: "35711699059");

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        var result = await ResponseValidationHelper.ValidateCreatedResponseAsync(response);
        var data = result.GetData();

        ContratoTestHelper.ValidateContratoResponse(data, "35711699059", 50000);
    }

    [Fact]
    public async Task Create_InvalidCpf_ReturnsBadRequest()
    {
        // Arrange
        var command = ContratoTestHelper.CreateDefaultContratoCommand(cpfCnpj: "123");

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        ResponseValidationHelper.ValidateBadRequestResponse(response.StatusCode);
    }

    [Fact]
    public async Task Create_NegativeValorTotal_ReturnsBadRequest()
    {
        // Arrange
        var command = ContratoTestHelper.CreateDefaultContratoCommand(valorTotal: -1000);

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        ResponseValidationHelper.ValidateBadRequestResponse(response.StatusCode);
    }

    [Fact]
    public async Task Create_PastDataVencimento_ReturnsBadRequest()
    {
        // Arrange
        var command = ContratoTestHelper.CreateDefaultContratoCommand(
            dataVencimentoPrimeiraParcela: DateTime.Today.AddDays(-1)
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/contratos", command);

        // Assert
        ResponseValidationHelper.ValidateBadRequestResponse(response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingContrato_ReturnsOk()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj: "65973205061");

        // Act
        var response = await Client.GetAsync($"/api/contratos/{contratoId}");

        // Assert
        var result = await ResponseValidationHelper.ValidateOkResponseAsync(response);
        var data = result.GetData();

        data.GetGuidValue("id").Should().Be(contratoId);
    }

    [Fact]
    public async Task GetById_NonExistingContrato_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/contratos/{nonExistingId}");

        // Assert
        ResponseValidationHelper.ValidateNotFoundResponse(response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingContrato_ReturnsNoContent()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj: "23894066024");

        // Act
        var response = await Client.DeleteAsync($"/api/contratos/{contratoId}");

        // Assert
        ResponseValidationHelper.ValidateNoContentResponse(response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingContrato_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/contratos/{nonExistingId}");

        // Assert
        ResponseValidationHelper.ValidateNotFoundResponse(response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        // Arrange
        var contratoId = await ContratoTestHelper.CreateContratoAsync(Client, cpfCnpj: "07349756003");

        // Act
        await Client.DeleteAsync($"/api/contratos/{contratoId}");
        var getResponse = await Client.GetAsync($"/api/contratos/{contratoId}");

        // Assert
        ResponseValidationHelper.ValidateNotFoundResponse(getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = Factory.CreateClient();
        var command = ContratoTestHelper.CreateDefaultContratoCommand();

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/contratos", command);

        // Assert
        ResponseValidationHelper.ValidateUnauthorizedResponse(response.StatusCode);
    }
}