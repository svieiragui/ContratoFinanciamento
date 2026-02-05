using ContractsApi.Domain.Common;
using ContractsApi.Domain.Enums;
using ContractsApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ContractsApi.Application.Features.Clientes.GetResumo;

public class GetResumoClienteHandler
{
    private readonly IContratoFinanciamentoRepository _contratoRepository;
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly ILogger<GetResumoClienteHandler> _logger;

    public GetResumoClienteHandler(
        IContratoFinanciamentoRepository contratoRepository,
        IPagamentoRepository pagamentoRepository,
        ILogger<GetResumoClienteHandler> logger)
    {
        _contratoRepository = contratoRepository;
        _pagamentoRepository = pagamentoRepository;
        _logger = logger;
    }

    public async Task<Result<ResumoClienteDto>> Handle(
        GetResumoClienteQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando GetResumo - CorrelationId: {CorrelationId}, CpfCnpj: {CpfCnpj}",
            query.CorrelationId, query.CpfCnpj);

        if (string.IsNullOrWhiteSpace(query.CpfCnpj))
        {
            _logger.LogError("Falha: CPF/CNPJ inválido ou vazio - CorrelationId: {CorrelationId}",
                query.CorrelationId);
            return Result<ResumoClienteDto>.Failure("CPF/CNPJ é obrigatório", 400);
        }

        var contratos = await _contratoRepository.GetByClienteCpfCnpjAsync(query.CpfCnpj, cancellationToken);
        var contratosList = contratos.ToList();

        if (!contratosList.Any())
        {
            _logger.LogError("Falha: Cliente não possui contratos - CorrelationId: {CorrelationId}, CpfCnpj: {CpfCnpj}",
                query.CorrelationId, query.CpfCnpj);
            return Result<ResumoClienteDto>.Failure("Cliente não possui contratos", 404);
        }

        var contratosAtivos = contratosList.Count(c => c.Status == StatusContrato.ATIVO);
        var totalParcelas = contratosList.Sum(c => c.PrazoMeses);
        var saldoDevedorConsolidado = contratosList.Sum(c => c.SaldoDevedor);

        // Buscar todos os pagamentos de todos os contratos
        var todasParcelas = new List<ParcelaInfo>();
        var parcelasPagas = 0;
        var parcelasEmDia = 0;

        foreach (var contrato in contratosList)
        {
            var pagamentos = await _pagamentoRepository.GetByContratoIdAsync(contrato.Id, cancellationToken);
            var pagamentosList = pagamentos.ToList();

            parcelasPagas += pagamentosList.Count;
            parcelasEmDia += pagamentosList.Count(p => new List<StatusPagamento>{ StatusPagamento.EM_DIA, StatusPagamento.ANTECIPADO }.Contains(p.Status));

            // Adicionar parcelas do contrato
            for (int i = 1; i <= contrato.PrazoMeses; i++)
            {
                var dataVencimento = contrato.DataVencimentoPrimeiraParcela.AddMonths(i - 1);
                var pagamento = pagamentosList.FirstOrDefault(p => p.NumeroParcela == i);

                todasParcelas.Add(new ParcelaInfo
                {
                    NumeroParcela = i,
                    DataVencimento = dataVencimento,
                    Paga = pagamento != null,
                    DataPagamento = pagamento?.DataPagamento
                });
            }
        }

        var hoje = DateTime.Today;
        var parcelasEmAtraso = todasParcelas.Count(p => !p.Paga && p.DataVencimento < hoje);
        var parcelasAVencer = todasParcelas.Count(p => !p.Paga && p.DataVencimento >= hoje);

        var percentualEmDia = parcelasPagas > 0
            ? Math.Round((decimal)parcelasEmDia / parcelasPagas * 100, 2)
            : 0;

        var resumo = new ResumoClienteDto(
            query.CpfCnpj,
            contratosAtivos,
            totalParcelas,
            parcelasPagas,
            parcelasEmAtraso,
            parcelasAVencer,
            percentualEmDia,
            saldoDevedorConsolidado
        );

        _logger.LogInformation("GetResumo finalizado com sucesso - CorrelationId: {CorrelationId}",
            query.CorrelationId);

        return Result<ResumoClienteDto>.Success(resumo);
    }

    private class ParcelaInfo
    {
        public int NumeroParcela { get; set; }
        public DateTime DataVencimento { get; set; }
        public bool Paga { get; set; }
        public DateTime? DataPagamento { get; set; }
    }
}

public record ResumoClienteDto(
    string CpfCnpj,
    int QuantidadeContratosAtivos,
    int TotalParcelas,
    int ParcelasPagas,
    int ParcelasEmAtraso,
    int ParcelasAVencer,
    decimal PercentualParcelasPagasEmDia,
    decimal SaldoDevedorConsolidado
);