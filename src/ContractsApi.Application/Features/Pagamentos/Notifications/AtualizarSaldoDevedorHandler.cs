using ContractsApi.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ContractsApi.Application.Features.Pagamentos.Notifications;

/// <summary>
/// Handler que processa a atualização do saldo devedor em background (fire-and-forget)
/// com retry policy de 3 tentativas
/// </summary>
public class AtualizarSaldoDevedorHandler : INotificationHandler<PagamentoRegistradoNotification>
{
    private readonly IContratoFinanciamentoRepository _contratoRepository;
    private readonly ILogger<AtualizarSaldoDevedorHandler> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public AtualizarSaldoDevedorHandler(
        IContratoFinanciamentoRepository contratoRepository,
        ILogger<AtualizarSaldoDevedorHandler> logger)
    {
        _contratoRepository = contratoRepository;
        _logger = logger;

        // Configurar retry policy: 3 tentativas com delay exponencial
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Tentativa {RetryCount} de atualização do saldo devedor falhou. " +
                        "Aguardando {DelaySeconds}s antes da próxima tentativa. " +
                        "ContratoId: {ContratoId}, PagamentoId: {PagamentoId}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        context["ContratoId"],
                        context["PagamentoId"]
                    );
                }
            );
    }

    public async Task Handle(PagamentoRegistradoNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Iniciando atualização assíncrona do saldo devedor. " +
            "ContratoId: {ContratoId}, PagamentoId: {PagamentoId}, AmortizacaoPaga: {AmortizacaoPaga}",
            notification.ContratoId,
            notification.PagamentoId,
            notification.AmortizacaoPaga
        );

        try
        {
            var context = new Context
            {
                ["ContratoId"] = notification.ContratoId,
                ["PagamentoId"] = notification.PagamentoId
            };

            await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                await AtualizarSaldoDevedorAsync(notification, cancellationToken);
            }, context);

            _logger.LogInformation(
                "Saldo devedor atualizado com sucesso. " +
                "ContratoId: {ContratoId}, PagamentoId: {PagamentoId}",
                notification.ContratoId,
                notification.PagamentoId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FALHA: Não foi possível atualizar o saldo devedor após 3 tentativas. " +
                "ContratoId: {ContratoId}, PagamentoId: {PagamentoId}, AmortizacaoPaga: {AmortizacaoPaga}. " +
                "AÇÃO NECESSÁRIA: Verificar manualmente e corrigir inconsistência.",
                notification.ContratoId,
                notification.PagamentoId,
                notification.AmortizacaoPaga
            );

            // IMPORTANTE: Não lançar a exceção para não quebrar o pipeline do MediatR
            // O pagamento já foi registrado com sucesso
            // TODO: Considerar implementar Outbox Pattern ou Dead Letter Queue para reprocessamento
        }
    }

    private async Task AtualizarSaldoDevedorAsync(
        PagamentoRegistradoNotification notification,
        CancellationToken cancellationToken)
    {
        var contrato = await _contratoRepository.GetByIdAsync(notification.ContratoId, cancellationToken);

        if (contrato == null)
        {
            throw new InvalidOperationException(
                $"Contrato não encontrado. ContratoId: {notification.ContratoId}"
            );
        }

        _logger.LogDebug(
            "Saldo devedor antes da atualização: {SaldoAnterior}. ContratoId: {ContratoId}",
            contrato.SaldoDevedor,
            notification.ContratoId
        );

        contrato.AtualizarSaldoDevedor(notification.AmortizacaoPaga);

        await _contratoRepository.UpdateAsync(contrato, cancellationToken);

        _logger.LogDebug(
            "Saldo devedor após atualização: {SaldoNovo}. ContratoId: {ContratoId}",
            contrato.SaldoDevedor,
            notification.ContratoId
        );
    }
}