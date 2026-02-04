using ContractsApi.Application.Features.Pagamentos.Create;
using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;

namespace ContractsApi.Application.Features.Pagamentos.GetByContrato;

public class GetPagamentosByContratoHandler
{
    private readonly IPagamentoRepository _repository;

    public GetPagamentosByContratoHandler(IPagamentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<PagamentoResponseDto>>> Handle(
        GetPagamentosByContratoQuery query,
        CancellationToken cancellationToken)
    {
        var pagamentos = await _repository.GetByContratoIdAsync(query.ContratoId, cancellationToken);

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

        return Result<IEnumerable<PagamentoResponseDto>>.Success(response);
    }
}