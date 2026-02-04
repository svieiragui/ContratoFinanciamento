using ContractsApi.Domain.Entities;

namespace ContractsApi.Domain.Repositories;

public interface IContratoFinanciamentoRepository
{
    Task<ContratoFinanciamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContratoFinanciamento>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ContratoFinanciamento>> GetByClienteCpfCnpjAsync(string cpfCnpj, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(ContratoFinanciamento contrato, CancellationToken cancellationToken = default);
    Task UpdateAsync(ContratoFinanciamento contrato, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}