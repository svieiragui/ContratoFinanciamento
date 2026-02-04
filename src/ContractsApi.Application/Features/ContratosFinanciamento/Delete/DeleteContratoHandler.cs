using ContractsApi.Domain.Common;
using ContractsApi.Domain.Repositories;

namespace ContractsApi.Application.Features.ContratosFinanciamento.Delete;

public class DeleteContratoHandler
{
    private readonly IContratoFinanciamentoRepository _repository;

    public DeleteContratoHandler(IContratoFinanciamentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(DeleteContratoCommand command, CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(command.Id, cancellationToken);

        if (!exists)
        {
            return Result<bool>.Failure("Contrato não encontrado", 404);
        }

        await _repository.DeleteAsync(command.Id, cancellationToken);

        return Result<bool>.Success(true, 204);
    }
}