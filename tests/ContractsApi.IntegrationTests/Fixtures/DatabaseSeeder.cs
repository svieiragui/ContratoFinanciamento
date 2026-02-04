using Npgsql;

namespace ContractsApi.IntegrationTests.Fixtures;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await CreateExtensionsAsync(connection);
        await CreateTablesAsync(connection);
        await CreateIndexesAsync(connection);
        await InsertSeedDataAsync(connection);
    }

    private static async Task CreateExtensionsAsync(NpgsqlConnection connection)
    {
        var sql = "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateTablesAsync(NpgsqlConnection connection)
    {
        var sql = @"
                    CREATE TABLE IF NOT EXISTS contracts (
                        id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                        name VARCHAR(255) NOT NULL,
                        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS contratos_financiamento (
                        id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                        cliente_cpf_cnpj VARCHAR(14) NOT NULL,
                        valor_total DECIMAL(18,2) NOT NULL,
                        taxa_mensal DECIMAL(5,2) NOT NULL,
                        prazo_meses INTEGER NOT NULL,
                        data_vencimento_primeira_parcela DATE NOT NULL,
                        tipo_veiculo INTEGER NOT NULL,
                        condicao_veiculo INTEGER NOT NULL,
                        status INTEGER NOT NULL DEFAULT 1,
                        valor_parcela DECIMAL(18,2) NOT NULL,
                        saldo_devedor DECIMAL(18,2) NOT NULL,
                        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS pagamentos (
                        id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                        contrato_id UUID NOT NULL,
                        numero_parcela INTEGER NOT NULL,
                        valor_pago DECIMAL(18,2) NOT NULL,
                        data_pagamento DATE NOT NULL,
                        data_vencimento DATE NOT NULL,
                        status INTEGER NOT NULL,
                        juros_pago DECIMAL(18,2) NOT NULL,
                        amortizacao_paga DECIMAL(18,2) NOT NULL,
                        saldo_devedor_apos DECIMAL(18,2) NOT NULL,
                        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT fk_pagamento_contrato FOREIGN KEY (contrato_id) 
                            REFERENCES contratos_financiamento(id) ON DELETE CASCADE
                    );";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateIndexesAsync(NpgsqlConnection connection)
    {
        var sql = @"
                    CREATE UNIQUE INDEX IF NOT EXISTS idx_contracts_name ON contracts(name);
                    CREATE INDEX IF NOT EXISTS idx_contratos_cliente ON contratos_financiamento(cliente_cpf_cnpj);
                    CREATE INDEX IF NOT EXISTS idx_contratos_status ON contratos_financiamento(status);
                    CREATE INDEX IF NOT EXISTS idx_pagamentos_contrato ON pagamentos(contrato_id);
                    CREATE UNIQUE INDEX IF NOT EXISTS idx_pagamentos_contrato_parcela ON pagamentos(contrato_id, numero_parcela);";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertSeedDataAsync(NpgsqlConnection connection)
    {
        await InsertContractsAsync(connection);
        await InsertContratosFinanciamentoAsync(connection);
        await InsertPagamentosAsync(connection);
    }

    private static async Task InsertContractsAsync(NpgsqlConnection connection)
    {
        var sql = @"
                    INSERT INTO contracts (id, name) VALUES 
                    ('550e8400-e29b-41d4-a716-446655440001', 'Contract Sample 1'),
                    ('550e8400-e29b-41d4-a716-446655440002', 'Contract Sample 2'),
                    ('550e8400-e29b-41d4-a716-446655440003', 'Contract Sample 3')
                    ON CONFLICT (name) DO NOTHING;";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertContratosFinanciamentoAsync(NpgsqlConnection connection)
    {
        var sql = @"
                    INSERT INTO contratos_financiamento (id, cliente_cpf_cnpj, valor_total, taxa_mensal, prazo_meses, 
                        data_vencimento_primeira_parcela, tipo_veiculo, condicao_veiculo, status, valor_parcela, saldo_devedor) 
                    VALUES 
                    ('650e8400-e29b-41d4-a716-446655440001', '12345678901', 50000.00, 2.50, 48, CURRENT_DATE + 30, 1, 1, 1, 1346.18, 50000.00),
                    ('650e8400-e29b-41d4-a716-446655440002', '12345678901', 30000.00, 2.00, 36, CURRENT_DATE + 15, 2, 2, 1, 996.56, 30000.00),
                    ('650e8400-e29b-41d4-a716-446655440003', '98765432100', 80000.00, 3.00, 60, CURRENT_DATE + 10, 3, 1, 1, 1946.74, 80000.00)
                    ON CONFLICT (id) DO NOTHING;";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertPagamentosAsync(NpgsqlConnection connection)
    {
        var sql = @"
                    INSERT INTO pagamentos (id, contrato_id, numero_parcela, valor_pago, data_pagamento, data_vencimento, 
                        status, juros_pago, amortizacao_paga, saldo_devedor_apos) 
                    VALUES 
                    ('750e8400-e29b-41d4-a716-446655440001', '650e8400-e29b-41d4-a716-446655440001', 1, 1346.18, CURRENT_DATE + 30, CURRENT_DATE + 30, 1, 1250.00, 96.18, 49903.82),
                    ('750e8400-e29b-41d4-a716-446655440002', '650e8400-e29b-41d4-a716-446655440001', 2, 1346.18, CURRENT_DATE + 55, CURRENT_DATE + 60, 2, 1247.60, 98.58, 49805.24)
                    ON CONFLICT (contrato_id, numero_parcela) DO NOTHING;";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task ClearAllDataAsync(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var sql = "TRUNCATE TABLE pagamentos, contratos_financiamento, contracts CASCADE;";
        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}