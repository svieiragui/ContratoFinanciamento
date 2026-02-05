using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Enums;
using ContractsApi.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using Moq;
using Microsoft.Extensions.Logging;

namespace ContractsApi.UnitTests.Features.ContratosFinanciamento;

public class CreateContratoHandlerTests
{
    private readonly Mock<IContratoFinanciamentoRepository> _repositoryMock;
    private readonly IValidator<CreateContratoCommand> _validator;
    private readonly Mock<ILogger<CreateContratoHandler>> _loggerMock;
    private readonly CreateContratoHandler _handler;

    public CreateContratoHandlerTests()
    {
        _repositoryMock = new Mock<IContratoFinanciamentoRepository>();
        _validator = new CreateContratoValidator();
        _loggerMock = new Mock<ILogger<CreateContratoHandler>>();
        _handler = new CreateContratoHandler(_repositoryMock.Object, _validator, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResult()
    {
        // Arrange
        var command = new CreateContratoCommand(
            "93838094000",
            50000,
            2.5m,
            48,
            DateTime.Today.AddDays(30),
            TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo.NOVO,
            Guid.NewGuid().ToString()
        );

        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<ContratoFinanciamento>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
        result.Data!.ValorParcela.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_InvalidCpf_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateContratoCommand(
            "123", // CPF inválido
            50000,
            2.5m,
            48,
            DateTime.Today.AddDays(30),
            TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo.NOVO,
            Guid.NewGuid().ToString()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_NegativeValorTotal_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateContratoCommand(
            "12345678901",
            -1000, // Valor negativo
            2.5m,
            48,
            DateTime.Today.AddDays(30),
            TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo.NOVO,
            Guid.NewGuid().ToString()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Valor total deve ser maior que zero");
    }

    [Fact]
    public async Task Handle_PastDataVencimento_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateContratoCommand(
            "12345678901",
            50000,
            2.5m,
            48,
            DateTime.Today.AddDays(-1), // Data no passado
            TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo.NOVO,
            Guid.NewGuid().ToString()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Data da primeira parcela deve ser futura");
    }
}