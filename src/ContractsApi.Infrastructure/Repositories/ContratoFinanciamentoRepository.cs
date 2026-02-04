using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Enums;
using ContractsApi.Domain.Repositories;
using Dapper;
using Npgsql;

namespace ContractsApi.Infrastructure.Repositories;

public class ContratoFinanciamentoRepository : IContratoFinanciamentoRepository
{
    private readonly string _connectionString;

    public ContratoFinanciamentoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ContratoFinanciamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT id, cliente_cpf_cnpj, valor_total, taxa_mensal, prazo_meses,
                   data_vencimento_primeira_parcela, tipo_veiculo, condicao_veiculo,
                   status, valor_parcela, saldo_devedor, created_at, updated_at
            FROM contratos_financiamento
            WHERE id = @Id";

        var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });

        return result == null ? null : MapToEntity(result);
    }

    public async Task<IEnumerable<ContratoFinanciamento>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT id, cliente_cpf_cnpj, valor_total, taxa_mensal, prazo_meses,
                   data_vencimento_primeira_parcela, tipo_veiculo, condicao_veiculo,
                   status, valor_parcela, saldo_devedor, created_at, updated_at
            FROM contratos_financiamento
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync<dynamic>(sql);

        return results.Select(MapToEntity);
    }

    public async Task<IEnumerable<ContratoFinanciamento>> GetByClienteCpfCnpjAsync(string cpfCnpj, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT id, cliente_cpf_cnpj, valor_total, taxa_mensal, prazo_meses,
                   data_vencimento_primeira_parcela, tipo_veiculo, condicao_veiculo,
                   status, valor_parcela, saldo_devedor, created_at, updated_at
            FROM contratos_financiamento
            WHERE cliente_cpf_cnpj = @CpfCnpj
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync<dynamic>(sql, new { CpfCnpj = cpfCnpj });

        return results.Select(MapToEntity);
    }

    public async Task<Guid> CreateAsync(ContratoFinanciamento contrato, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            INSERT INTO contratos_financiamento (
                id, cliente_cpf_cnpj, valor_total, taxa_mensal, prazo_meses,
                data_vencimento_primeira_parcela, tipo_veiculo, condicao_veiculo,
                status, valor_parcela, saldo_devedor, created_at, updated_at
            ) VALUES (
                @Id, @ClienteCpfCnpj, @ValorTotal, @TaxaMensal, @PrazoMeses,
                @DataVencimentoPrimeiraParcela, @TipoVeiculo, @CondicaoVeiculo,
                @Status, @ValorParcela, @SaldoDevedor, @CreatedAt, @UpdatedAt
            )
            RETURNING id";

        var id = await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            contrato.Id,
            contrato.ClienteCpfCnpj,
            contrato.ValorTotal,
            contrato.TaxaMensal,
            contrato.PrazoMeses,
            contrato.DataVencimentoPrimeiraParcela,
            TipoVeiculo = (int)contrato.TipoVeiculo,
            CondicaoVeiculo = (int)contrato.CondicaoVeiculo,
            Status = (int)contrato.Status,
            contrato.ValorParcela,
            contrato.SaldoDevedor,
            contrato.CreatedAt,
            contrato.UpdatedAt
        });

        return id;
    }

    public async Task UpdateAsync(ContratoFinanciamento contrato, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            UPDATE contratos_financiamento
            SET status = @Status,
                saldo_devedor = @SaldoDevedor,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            contrato.Id,
            Status = (int)contrato.Status,
            contrato.SaldoDevedor,
            contrato.UpdatedAt
        });
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = "DELETE FROM contratos_financiamento WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = "SELECT COUNT(1) FROM contratos_financiamento WHERE id = @Id";

        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });

        return count > 0;
    }

    private static ContratoFinanciamento MapToEntity(dynamic row)
    {
        // Converter DateOnly para DateTime se necessário
        DateTime dataVencimento = row.data_vencimento_primeira_parcela is DateOnly dateOnly
            ? dateOnly.ToDateTime(TimeOnly.MinValue)
            : (DateTime)row.data_vencimento_primeira_parcela;

        var contrato = typeof(ContratoFinanciamento)
            .GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[]
            {
                (string)row.cliente_cpf_cnpj,
                (decimal)row.valor_total,
                (decimal)row.taxa_mensal,
                (int)row.prazo_meses,
                dataVencimento,
                (TipoVeiculo)(int)row.tipo_veiculo,
                (CondicaoVeiculo)(int)row.condicao_veiculo
            }) as ContratoFinanciamento;

        typeof(ContratoFinanciamento).GetProperty("Id")!.SetValue(contrato, (Guid)row.id);
        typeof(ContratoFinanciamento).GetProperty("Status")!.SetValue(contrato, (StatusContrato)(int)row.status);
        typeof(ContratoFinanciamento).GetProperty("SaldoDevedor")!.SetValue(contrato, (decimal)row.saldo_devedor);
        typeof(ContratoFinanciamento).GetProperty("CreatedAt")!.SetValue(contrato, (DateTime)row.created_at);
        typeof(ContratoFinanciamento).GetProperty("UpdatedAt")!.SetValue(contrato, (DateTime)row.updated_at);

        return contrato!;
    }
}