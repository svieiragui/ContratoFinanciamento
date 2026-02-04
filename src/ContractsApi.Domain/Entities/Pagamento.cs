using ContractsApi.Domain.Enums;

namespace ContractsApi.Domain.Entities;

public class Pagamento
{
    public Guid Id { get; private set; }
    public Guid ContratoId { get; private set; }
    public int NumeroParcela { get; private set; }
    public decimal ValorPago { get; private set; }
    public DateTime DataPagamento { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public StatusPagamento Status { get; private set; }
    public decimal JurosPago { get; private set; }
    public decimal AmortizacaoPaga { get; private set; }
    public decimal SaldoDevedorAposPaymento { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Pagamento() { }

    public static Pagamento Create(
        Guid contratoId,
        int numeroParcela,
        decimal valorPago,
        DateTime dataPagamento,
        DateTime dataVencimento,
        decimal juros,
        decimal amortizacao,
        decimal saldoDevedorApos)
    {
        var status = DeterminarStatus(dataPagamento, dataVencimento);

        return new Pagamento
        {
            Id = Guid.NewGuid(),
            ContratoId = contratoId,
            NumeroParcela = numeroParcela,
            ValorPago = valorPago,
            DataPagamento = dataPagamento,
            DataVencimento = dataVencimento,
            Status = status,
            JurosPago = juros,
            AmortizacaoPaga = amortizacao,
            SaldoDevedorAposPaymento = saldoDevedorApos,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static StatusPagamento DeterminarStatus(DateTime dataPagamento, DateTime dataVencimento)
    {
        if (dataPagamento.Date < dataVencimento.Date)
            return StatusPagamento.ANTECIPADO;

        if (dataPagamento.Date > dataVencimento.Date)
            return StatusPagamento.EM_ATRASO;

        return StatusPagamento.EM_DIA;
    }
}