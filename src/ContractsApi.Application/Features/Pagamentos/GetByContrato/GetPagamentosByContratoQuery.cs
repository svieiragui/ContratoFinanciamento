namespace ContractsApi.Application.Features.Pagamentos.GetByContrato;

public record GetPagamentosByContratoQuery(Guid ContratoId, string CorrelationId);