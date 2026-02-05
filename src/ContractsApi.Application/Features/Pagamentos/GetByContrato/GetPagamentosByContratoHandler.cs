using ContractsApi.Application.Features.Pagamentos.Create;
using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ContractsApi.Application.Features.Pagamentos.GetByContrato;

public class GetPagamentosByContratoHandler
{
    private readonly IPagamentoRepository _repository;
    private readonly ILogger<GetPagamentosByContratoHandler> _logger;

    public GetPagamentosByContratoHandler(
        IPagamentoRepository repository,
        ILogger<GetPagamentosByContratoHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PagamentoResponseDto>>> Handle(
        GetPagamentosByContratoQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando GetPagamentosByContrato - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
            query.CorrelationId, query.ContratoId);

        var pagamentos = await _repository.GetByContratoIdAsync(query.ContratoId, cancellationToken);

        if (!pagamentos.Any())
        {
            _logger.LogWarning("Nenhum pagamento encontrado - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
                query.CorrelationId, query.ContratoId);
        }

        var response = pagamentos.Select(p => new PagamentoResponseDto(
            p.Id,
            p.ContratoId,
            p.NumeroParcela,
            p.ValorPago,
            p.DataPagamento,
            p.DataVencimento,
            p.Status.ToString(),
            p.JurosPago,
            p.AmortizacaoPaga,
            p.SaldoDevedorAposPaymento
        ));

        _logger.LogInformation("GetPagamentosByContrato finalizado com sucesso - CorrelationId: {CorrelationId}, Total: {Total}",
            query.CorrelationId, response.Count());

        return Result<IEnumerable<PagamentoResponseDto>>.Success(response);
    }
}