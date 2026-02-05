using ContractsApi.Application.Features.Pagamentos.Notifications;
using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Enums;
using ContractsApi.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContractsApi.UnitTests.Features.Pagamentos;

public class AtualizarSaldoDevedorHandlerTests
{
    private readonly Mock<IContratoFinanciamentoRepository> _contratoRepositoryMock;
    private readonly Mock<ILogger<AtualizarSaldoDevedorHandler>> _loggerMock;
    private readonly AtualizarSaldoDevedorHandler _handler;

    public AtualizarSaldoDevedorHandlerTests()
    {
        _contratoRepositoryMock = new Mock<IContratoFinanciamentoRepository>();
        _loggerMock = new Mock<ILogger<AtualizarSaldoDevedorHandler>>();
        _handler = new AtualizarSaldoDevedorHandler(_contratoRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ContratoExiste_AtualizaSaldoDevedorComSucesso()
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

        var notification = new PagamentoRegistradoNotification(
            PagamentoId: Guid.NewGuid(),
            ContratoId: contratoId,
            NumeroParcela: 1,
            ValorPago: 1346.18m,
            AmortizacaoPaga: 96.18m,
            DataRegistro: DateTime.UtcNow
        );

        _contratoRepositoryMock
            .Setup(x => x.GetByIdAsync(contratoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contrato);

        var saldoInicial = contrato.SaldoDevedor;

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _contratoRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ContratoFinanciamento>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        contrato.SaldoDevedor.Should().Be(saldoInicial - notification.AmortizacaoPaga);
    }

    [Fact]
    public async Task Handle_ContratoNaoEncontrado_LogaErroENaoLancaExcecao()
    {
        // Arrange
        var notification = new PagamentoRegistradoNotification(
            PagamentoId: Guid.NewGuid(),
            ContratoId: Guid.NewGuid(),
            NumeroParcela: 1,
            ValorPago: 1346.18m,
            AmortizacaoPaga: 96.18m,
            DataRegistro: DateTime.UtcNow
        );

        _contratoRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContratoFinanciamento?)null);

        // Act
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert - Não deve lançar exceção
        await act.Should().NotThrowAsync();

        // Verifica que o erro foi logado
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_FalhaTemporaria_RealizaRetry()
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

        var notification = new PagamentoRegistradoNotification(
            PagamentoId: Guid.NewGuid(),
            ContratoId: contratoId,
            NumeroParcela: 1,
            ValorPago: 1346.18m,
            AmortizacaoPaga: 96.18m,
            DataRegistro: DateTime.UtcNow
        );

        var callCount = 0;
        _contratoRepositoryMock
            .Setup(x => x.GetByIdAsync(contratoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contrato);

        _contratoRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ContratoFinanciamento>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 2) // Falha na primeira tentativa
                    throw new Exception("Erro temporário de conexão");
                return Task.CompletedTask;
            });

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Deve ter tentado 2 vezes (1 falha + 1 sucesso)
        _contratoRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ContratoFinanciamento>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );

        // Verifica que o warning de retry foi logado
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_FalhaApos3Tentativas_LogaErroENaoLancaExcecao()
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

        var notification = new PagamentoRegistradoNotification(
            PagamentoId: Guid.NewGuid(),
            ContratoId: contratoId,
            NumeroParcela: 1,
            ValorPago: 1346.18m,
            AmortizacaoPaga: 96.18m,
            DataRegistro: DateTime.UtcNow
        );

        _contratoRepositoryMock
            .Setup(x => x.GetByIdAsync(contratoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contrato);

        _contratoRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ContratoFinanciamento>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro persistente"));

        // Act
        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        // Assert - Não deve lançar exceção
        await act.Should().NotThrowAsync();

        // Deve ter tentado 4 vezes (1 original + 3 retries)
        _contratoRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ContratoFinanciamento>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4)
        );

        // Verifica que o erro final foi logado
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}