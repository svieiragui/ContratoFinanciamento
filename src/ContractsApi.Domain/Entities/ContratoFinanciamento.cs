using ContractsApi.Domain.Enums;

namespace ContractsApi.Domain.Entities;

public class ContratoFinanciamento
{
    public Guid Id { get; private set; }
    public string ClienteCpfCnpj { get; private set; } = string.Empty;
    public decimal ValorTotal { get; private set; }
    public decimal TaxaMensal { get; private set; }
    public int PrazoMeses { get; private set; }
    public DateTime DataVencimentoPrimeiraParcela { get; private set; }
    public TipoVeiculo TipoVeiculo { get; private set; }
    public CondicaoVeiculo CondicaoVeiculo { get; private set; }
    public StatusContrato Status { get; private set; }
    public decimal ValorParcela { get; private set; }
    public decimal SaldoDevedor { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ContratoFinanciamento() { }

    public static ContratoFinanciamento Create(
        string clienteCpfCnpj,
        decimal valorTotal,
        decimal taxaMensal,
        int prazoMeses,
        DateTime dataVencimentoPrimeiraParcela,
        TipoVeiculo tipoVeiculo,
        CondicaoVeiculo condicaoVeiculo)
    {
        var valorParcela = CalcularValorParcela(valorTotal, taxaMensal, prazoMeses);

        return new ContratoFinanciamento
        {
            Id = Guid.NewGuid(),
            ClienteCpfCnpj = clienteCpfCnpj,
            ValorTotal = valorTotal,
            TaxaMensal = taxaMensal,
            PrazoMeses = prazoMeses,
            DataVencimentoPrimeiraParcela = dataVencimentoPrimeiraParcela,
            TipoVeiculo = tipoVeiculo,
            CondicaoVeiculo = condicaoVeiculo,
            Status = StatusContrato.ATIVO,
            ValorParcela = valorParcela,
            SaldoDevedor = valorTotal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AtualizarSaldoDevedor(decimal valorPago)
    {
        SaldoDevedor -= valorPago;
        UpdatedAt = DateTime.UtcNow;

        if (SaldoDevedor <= 0)
        {
            SaldoDevedor = 0;
            Status = StatusContrato.QUITADO;
        }
    }

    public void Cancelar()
    {
        Status = StatusContrato.CANCELADO;
        UpdatedAt = DateTime.UtcNow;
    }

    private static decimal CalcularValorParcela(decimal valorTotal, decimal taxaMensal, int prazoMeses)
    {
        // Fórmula Price: P = V * (i * (1 + i)^n) / ((1 + i)^n - 1)
        var taxaDecimal = taxaMensal / 100;
        var fator = Math.Pow((double)(1 + taxaDecimal), prazoMeses);
        var valorParcela = valorTotal * (taxaDecimal * (decimal)fator) / ((decimal)fator - 1);

        return Math.Round(valorParcela, 2);
    }

    public decimal CalcularJurosPeriodo(int numeroParcela)
    {
        // Juros = Saldo Devedor Anterior * Taxa Mensal
        var saldoAnterior = SaldoDevedor + (ValorParcela * (PrazoMeses - numeroParcela + 1)) - ValorTotal;
        return Math.Round(saldoAnterior * (TaxaMensal / 100), 2);
    }
}