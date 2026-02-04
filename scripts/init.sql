-- Criar extensão para UUID
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tabela de Contratos de Financiamento
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

-- Índices para Contratos
CREATE INDEX IF NOT EXISTS idx_contratos_cliente ON contratos_financiamento(cliente_cpf_cnpj);
CREATE INDEX IF NOT EXISTS idx_contratos_status ON contratos_financiamento(status);

-- Tabela de Pagamentos
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
    CONSTRAINT fk_pagamento_contrato FOREIGN KEY (contrato_id) REFERENCES contratos_financiamento(id) ON DELETE CASCADE
);

-- Índices para Pagamentos
CREATE INDEX IF NOT EXISTS idx_pagamentos_contrato ON pagamentos(contrato_id);
CREATE INDEX IF NOT EXISTS idx_pagamentos_status ON pagamentos(status);
CREATE UNIQUE INDEX IF NOT EXISTS idx_pagamentos_contrato_parcela ON pagamentos(contrato_id, numero_parcela);

-- Tabela antiga de Contracts (manter para compatibilidade)
CREATE TABLE IF NOT EXISTS contracts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_contracts_name ON contracts(name);

-- Seed inicial de contratos
INSERT INTO contracts (id, name) VALUES 
    ('550e8400-e29b-41d4-a716-446655440001', 'Contract Sample 1'),
    ('550e8400-e29b-41d4-a716-446655440002', 'Contract Sample 2'),
    ('550e8400-e29b-41d4-a716-446655440003', 'Contract Sample 3')
ON CONFLICT (name) DO NOTHING;

-- Seed inicial de contratos de financiamento
INSERT INTO contratos_financiamento (
    id, cliente_cpf_cnpj, valor_total, taxa_mensal, prazo_meses, 
    data_vencimento_primeira_parcela, tipo_veiculo, condicao_veiculo, 
    status, valor_parcela, saldo_devedor
) VALUES 
    (
        '650e8400-e29b-41d4-a716-446655440001',
        '12345678901',
        50000.00,
        2.50,
        48,
        '2026-03-01',
        1, -- AUTOMOVEL
        1, -- NOVO
        1, -- ATIVO
        1346.18,
        50000.00
    ),
    (
        '650e8400-e29b-41d4-a716-446655440002',
        '12345678901',
        30000.00,
        2.00,
        36,
        '2026-02-15',
        2, -- MOTO
        2, -- SEMINOVO
        1, -- ATIVO
        996.56,
        30000.00
    ),
    (
        '650e8400-e29b-41d4-a716-446655440003',
        '98765432100',
        80000.00,
        3.00,
        60,
        '2026-03-10',
        3, -- CAMINHAO
        1, -- NOVO
        1, -- ATIVO
        1946.74,
        80000.00
    )
ON CONFLICT (id) DO NOTHING;

-- Seed inicial de pagamentos
INSERT INTO pagamentos (
    contrato_id, numero_parcela, valor_pago, data_pagamento, 
    data_vencimento, status, juros_pago, amortizacao_paga, saldo_devedor_apos
) VALUES 
    (
        '650e8400-e29b-41d4-a716-446655440001',
        1,
        1346.18,
        '2026-03-01',
        '2026-03-01',
        1, -- EM_DIA
        1250.00,
        96.18,
        49903.82
    ),
    (
        '650e8400-e29b-41d4-a716-446655440001',
        2,
        1346.18,
        '2026-03-28',
        '2026-04-01',
        2, -- ANTECIPADO
        1247.60,
        98.58,
        49805.24
    )
ON CONFLICT (contrato_id, numero_parcela) DO NOTHING;