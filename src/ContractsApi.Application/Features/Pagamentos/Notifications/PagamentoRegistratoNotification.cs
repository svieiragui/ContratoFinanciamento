using MediatR;

namespace ContractsApi.Application.Features.Pagamentos.Notifications;

public record PagamentoRegistradoNotification(
    Guid PagamentoId,
    Guid ContratoId,
    int NumeroParcela,
    decimal ValorPago,
    decimal AmortizacaoPaga,
    DateTime DataRegistro
) : INotification;