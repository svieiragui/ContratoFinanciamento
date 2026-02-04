using ContractsApi.Application.Features.Pagamentos.Create;
using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Enums;
using ContractsApi.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace ContractsApi.UnitTests.Features.Pagamentos;

public class CreatePagamentoHandlerTests
{
    private readonly Mock<IPagamentoRepository> _pagamentoRepositoryMock;
    private readonly Mock<IContratoFinanciamentoRepository> _contratoRepositoryMock;
    private readonly IValidator<CreatePagamentoCommand> _validator;
    private readonly CreatePagamentoHandler _handler;

    public CreatePagamentoHandlerTests()
    {
        _pagamentoRepositoryMock = new Mock<IPagamentoRepository>();
        _contratoRepositoryMock = new Mock<IContratoFinanciamentoRepository>();
        _validator = new CreatePagamentoValidator();
        _handler = new CreatePagamentoHandler(
            _pagamentoRepositoryMock.Object,
            _contratoRepositoryMock.Object,
            _validator);
    }

    [Fact]
    public async Task Handle_ValidPayment_ReturnsSuccessResult()
    {
        // Arrange
        var contratoId = Guid.NewGuid();
        var contrato = ContratoFinanciamento.Create(
            "12345678901",
            50000,
            2.5m,
            48,
            DateTime.Today.AddDays(30),
            TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo.NOVO
        );

        typeof(ContratoFinanciamento).GetProperty("Id")!.SetValue(contrato, contratoId);

        var command = new CreatePagamentoCommand(
            contratoId,
            1,
            contrato.ValorParcela,
            DateTime.Today
        );

        _contratoRepositoryMock.Setup(x => x.GetByIdAsync(contratoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contrato);

        _pagamentoRepositoryMock.Setup(x => x.GetByContratoAndParcelaAsync(contratoId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pagamento?)null);

        _pagamentoRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Pagamento>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_ContratoNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var command = new CreatePagamentoCommand(
            Guid.NewGuid(),
            1,
            1000,
            DateTime.Today
        );

        _contratoRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContratoFinanciamento?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Contain("Contrato não encontrado");
    }

    [Fact]
    public async Task Handle_ParcelaAlreadyPaid_ReturnsConflictError()
    {
        // Arrange
        var contratoId = Guid.NewGuid();
        var contrato = ContratoFinanciamento.Create(
            "12345678901",
            50000,
            2.5m,
            48,
            DateTime.Today.AddDays(30),
            TipoVeiculo.AUTOMOVEL,
            CondicaoVeiculo.NOVO
        );

        typeof(ContratoFinanciamento).GetProperty("Id")!.SetValue(contrato, contratoId);

        var pagamentoExistente = Pagamento.Create(
            contratoId,
            1,
            1000,
            DateTime.Today,
            DateTime.Today.AddDays(30),
            100,
            900,
            49000
        );

        var command = new CreatePagamentoCommand(
            contratoId,
            1,
            1000,
            DateTime.Today
        );

        _contratoRepositoryMock.Setup(x => x.GetByIdAsync(contratoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contrato);

        _pagamentoRepositoryMock.Setup(x => x.GetByContratoAndParcelaAsync(contratoId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamentoExistente);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.ErrorMessage.Should().Contain("Parcela já foi paga");
    }
}