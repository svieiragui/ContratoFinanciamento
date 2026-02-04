using ContractsApi.Domain.Entities;

namespace ContractsApi.Domain.Repositories;

public interface IPagamentoRepository
{
    Task<Pagamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Pagamento>> GetByContratoIdAsync(Guid contratoId, CancellationToken cancellationToken = default);
    Task<Pagamento?> GetByContratoAndParcelaAsync(Guid contratoId, int numeroParcela, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Pagamento pagamento, CancellationToken cancellationToken = default);
    Task<int> CountParcelasPagasByContratoAsync(Guid contratoId, CancellationToken cancellationToken = default);
}