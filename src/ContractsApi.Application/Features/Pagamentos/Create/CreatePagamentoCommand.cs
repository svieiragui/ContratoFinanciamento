namespace ContractsApi.Application.Features.Pagamentos.Create;

public record CreatePagamentoCommand(
    Guid ContratoId,
    int NumeroParcela,
    decimal ValorPago,
    DateTime DataPagamento,
    string CorrelationId
);