using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ContractsApi.Application.Features.ContratosFinanciamento.GetAll;

public class GetAllContratosHandler
{
    private readonly IContratoFinanciamentoRepository _repository;
    private readonly ILogger<GetAllContratosHandler> _logger;

    public GetAllContratosHandler(
        IContratoFinanciamentoRepository repository,
        ILogger<GetAllContratosHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ContratoResponseDto>>> Handle(
        GetAllContratosQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando GetAllContratos - CorrelationId: {CorrelationId}",
            query.CorrelationId);

        var contratos = await _repository.GetAllAsync(cancellationToken);

        if (!contratos.Any())
        {
            _logger.LogWarning("Nenhum contrato encontrado - CorrelationId: {CorrelationId}",
                query.CorrelationId);
        }

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

        _logger.LogInformation("GetAllContratos finalizado com sucesso - CorrelationId: {CorrelationId}, Total: {Total}",
            query.CorrelationId, response.Count());

        return Result<IEnumerable<ContratoResponseDto>>.Success(response);
    }
}