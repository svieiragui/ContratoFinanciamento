using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;

namespace ContractsApi.Application.Features.ContratosFinanciamento.GetById;

public class GetContratoByIdHandler
{
    private readonly IContratoFinanciamentoRepository _repository;

    public GetContratoByIdHandler(IContratoFinanciamentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ContratoResponseDto>> Handle(
        GetContratoByIdQuery query,
        CancellationToken cancellationToken)
    {
        var contrato = await _repository.GetByIdAsync(query.Id, cancellationToken);

        if (contrato == null)
        {
            return Result<ContratoResponseDto>.Failure("Contrato não encontrado", 404);
        }

        var response = new ContratoResponseDto(
            contrato.Id,
            contrato.ClienteCpfCnpj,
            contrato.ValorTotal,
            contrato.TaxaMensal,
            contrato.PrazoMeses,
            contrato.DataVencimentoPrimeiraParcela,
            contrato.TipoVeiculo.ToString(),
            contrato.CondicaoVeiculo.ToString(),
            contrato.Status.ToString(),
            contrato.ValorParcela,
            contrato.SaldoDevedor
        );

        return Result<ContratoResponseDto>.Success(response);
    }
}