namespace ContractsApi.Application.Features.Pagamentos.Create;

public record CreatePagamentoInput(
    int NumeroParcela,
    decimal ValorPago,
    DateTime DataPagamento
);
