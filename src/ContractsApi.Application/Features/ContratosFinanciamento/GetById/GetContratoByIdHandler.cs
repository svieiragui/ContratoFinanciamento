using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ContractsApi.Application.Features.ContratosFinanciamento.GetById;

public class GetContratoByIdHandler
{
    private readonly IContratoFinanciamentoRepository _repository;
    private readonly ILogger<GetContratoByIdHandler> _logger;

    public GetContratoByIdHandler(
        IContratoFinanciamentoRepository repository,
        ILogger<GetContratoByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ContratoResponseDto>> Handle(
        GetContratoByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando GetContratoById - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
            query.CorrelationId, query.Id);

        var contrato = await _repository.GetByIdAsync(query.Id, cancellationToken);

        if (contrato == null)
        {
            _logger.LogError("Falha: Contrato não encontrado - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
                query.CorrelationId, query.Id);
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

        _logger.LogInformation("GetContratoById finalizado com sucesso - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
            query.CorrelationId, query.Id);

        return Result<ContratoResponseDto>.Success(response);
    }
}