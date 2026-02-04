using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;

namespace ContractsApi.Application.Features.ContratosFinanciamento.GetAll;

public class GetAllContratosHandler
{
    private readonly IContratoFinanciamentoRepository _repository;

    public GetAllContratosHandler(IContratoFinanciamentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<ContratoResponseDto>>> Handle(
        GetAllContratosQuery query,
        CancellationToken cancellationToken)
    {
        var contratos = await _repository.GetAllAsync(cancellationToken);

        var response = contratos.Select(c => new ContratoResponseDto(
            c.Id,
            c.ClienteCpfCnpj,
            c.ValorTotal,
            c.TaxaMensal,
            c.PrazoMeses,
            c.DataVencimentoPrimeiraParcela,
            c.TipoVeiculo.ToString(),
            c.CondicaoVeiculo.ToString(),
            c.Status.ToString(),
            c.ValorParcela,
            c.SaldoDevedor
        ));

        return Result<IEnumerable<ContratoResponseDto>>.Success(response);
    }
}