using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ContractsApi.Application.Features.ContratosFinanciamento.Delete;

public class DeleteContratoHandler
{
    private readonly IContratoFinanciamentoRepository _repository;
    private readonly ILogger<DeleteContratoHandler> _logger;

    public DeleteContratoHandler(
        IContratoFinanciamentoRepository repository,
        ILogger<DeleteContratoHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteContratoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando DeleteContrato - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
            command.CorrelationId, command.Id);

        var exists = await _repository.ExistsAsync(command.Id, cancellationToken);

        if (!exists)
        {
            _logger.LogError("Falha: Contrato não encontrado - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
                command.CorrelationId, command.Id);
            return Result<bool>.Failure("Contrato não encontrado", 404);
        }

        await _repository.DeleteAsync(command.Id, cancellationToken);

        _logger.LogInformation("DeleteContrato finalizado com sucesso - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
            command.CorrelationId, command.Id);

        return Result<bool>.Success(true, 204);
    }
}