using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Enums;
using ContractsApi.Domain.Repositories;
using Dapper;
using Npgsql;

namespace ContractsApi.Infrastructure.Repositories;

public class PagamentoRepository : IPagamentoRepository
{
    private readonly string _connectionString;

    public PagamentoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Pagamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT id, contrato_id, numero_parcela, valor_pago, data_pagamento,
                   data_vencimento, status, juros_pago, amortizacao_paga,
                   saldo_devedor_apos, created_at
            FROM pagamentos
            WHERE id = @Id";

        var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });

        return result == null ? null : MapToEntity(result);
    }

    public async Task<IEnumerable<Pagamento>> GetByContratoIdAsync(Guid contratoId, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT id, contrato_id, numero_parcela, valor_pago, data_pagamento,
                   data_vencimento, status, juros_pago, amortizacao_paga,
                   saldo_devedor_apos, created_at
            FROM pagamentos
            WHERE contrato_id = @ContratoId
            ORDER BY numero_parcela";

        var results = await connection.QueryAsync<dynamic>(sql, new { ContratoId = contratoId });

        return results.Select(MapToEntity);
    }

    public async Task<Pagamento?> GetByContratoAndParcelaAsync(Guid contratoId, int numeroParcela, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT id, contrato_id, numero_parcela, valor_pago, data_pagamento,
                   data_vencimento, status, juros_pago, amortizacao_paga,
                   saldo_devedor_apos, created_at
            FROM pagamentos
            WHERE contrato_id = @ContratoId AND numero_parcela = @NumeroParcela";

        var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new
        {
            ContratoId = contratoId,
            NumeroParcela = numeroParcela
        });

        return result == null ? null : MapToEntity(result);
    }

    public async Task<Guid> CreateAsync(Pagamento pagamento, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            INSERT INTO pagamentos (
                id, contrato_id, numero_parcela, valor_pago, data_pagamento,
                data_vencimento, status, juros_pago, amortizacao_paga,
                saldo_devedor_apos, created_at
            ) VALUES (
                @Id, @ContratoId, @NumeroParcela, @ValorPago, @DataPagamento,
                @DataVencimento, @Status, @JurosPago, @AmortizacaoPaga,
                @SaldoDevedorAposPaymento, @CreatedAt
            )
            RETURNING id";

        var id = await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            pagamento.Id,
            pagamento.ContratoId,
            pagamento.NumeroParcela,
            pagamento.ValorPago,
            pagamento.DataPagamento,
            pagamento.DataVencimento,
            Status = (int)pagamento.Status,
            pagamento.JurosPago,
            pagamento.AmortizacaoPaga,
            pagamento.SaldoDevedorAposPaymento,
            pagamento.CreatedAt
        });

        return id;
    }

    public async Task<int> CountParcelasPagasByContratoAsync(Guid contratoId, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = "SELECT COUNT(*) FROM pagamentos WHERE contrato_id = @ContratoId";

        return await connection.ExecuteScalarAsync<int>(sql, new { ContratoId = contratoId });
    }

    private static Pagamento MapToEntity(dynamic row)
    {
        var pagamento = typeof(Pagamento)
            .GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[]
            {
                (Guid)row.contrato_id,
                (int)row.numero_parcela,
                (decimal)row.valor_pago,
                (DateTime)row.data_pagamento,
                (DateTime)row.data_vencimento,
                (decimal)row.juros_pago,
                (decimal)row.amortizacao_paga,
                (decimal)row.saldo_devedor_apos
            }) as Pagamento;

        typeof(Pagamento).GetProperty("Id")!.SetValue(pagamento, (Guid)row.id);
        typeof(Pagamento).GetProperty("CreatedAt")!.SetValue(pagamento, (DateTime)row.created_at);

        return pagamento!;
    }
}