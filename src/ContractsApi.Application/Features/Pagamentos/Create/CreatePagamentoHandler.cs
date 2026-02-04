using ContractsApi.Domain.Common;
using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Repositories;
using FluentValidation;

namespace ContractsApi.Application.Features.Pagamentos.Create;

public class CreatePagamentoHandler
{
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly IContratoFinanciamentoRepository _contratoRepository;
    private readonly IValidator<CreatePagamentoCommand> _validator;

    public CreatePagamentoHandler(
        IPagamentoRepository pagamentoRepository,
        IContratoFinanciamentoRepository contratoRepository,
        IValidator<CreatePagamentoCommand> validator)
    {
        _pagamentoRepository = pagamentoRepository;
        _contratoRepository = contratoRepository;
        _validator = validator;
    }

    public async Task<Result<PagamentoResponseDto>> Handle(
        CreatePagamentoCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<PagamentoResponseDto>.Failure(errors, 400);
        }

        // Verificar se contrato existe
        var contrato = await _contratoRepository.GetByIdAsync(command.ContratoId, cancellationToken);
        if (contrato == null)
        {
            return Result<PagamentoResponseDto>.Failure("Contrato não encontrado", 404);
        }

        // Verificar se parcela já foi paga
        var pagamentoExistente = await _pagamentoRepository.GetByContratoAndParcelaAsync(
            command.ContratoId,
            command.NumeroParcela,
            cancellationToken);

        if (pagamentoExistente != null)
        {
            return Result<PagamentoResponseDto>.Failure("Parcela já foi paga", 409);
        }

        // Validar número da parcela
        if (command.NumeroParcela > contrato.PrazoMeses)
        {
            return Result<PagamentoResponseDto>.Failure(
                $"Número da parcela não pode exceder o prazo de {contrato.PrazoMeses} meses",
                400);
        }

        // Calcular data de vencimento
        var dataVencimento = contrato.DataVencimentoPrimeiraParcela.AddMonths(command.NumeroParcela - 1);

        // Calcular juros e amortização
        var juros = contrato.CalcularJurosPeriodo(command.NumeroParcela);
        var amortizacao = command.ValorPago - juros;

        // Calcular novo saldo devedor
        var novoSaldoDevedor = contrato.SaldoDevedor - amortizacao;

        // Criar pagamento
        var pagamento = Pagamento.Create(
            command.ContratoId,
            command.NumeroParcela,
            command.ValorPago,
            command.DataPagamento,
            dataVencimento,
            juros,
            amortizacao,
            novoSaldoDevedor
        );

        await _pagamentoRepository.CreateAsync(pagamento, cancellationToken);

        // Atualizar saldo devedor do contrato
        contrato.AtualizarSaldoDevedor(amortizacao);
        await _contratoRepository.UpdateAsync(contrato, cancellationToken);

        var response = new PagamentoResponseDto(
            pagamento.Id,
            pagamento.ContratoId,
            pagamento.NumeroParcela,
            pagamento.ValorPago,
            pagamento.DataPagamento,
            pagamento.DataVencimento,
            pagamento.Status.ToString(),
            pagamento.JurosPago,
            pagamento.AmortizacaoPaga,
            pagamento.SaldoDevedorAposPaymento
        );

        return Result<PagamentoResponseDto>.Success(response, 201);
    }
}

public record PagamentoResponseDto(
    Guid Id,
    Guid ContratoId,
    int NumeroParcela,
    decimal ValorPago,
    DateTime DataPagamento,
    DateTime DataVencimento,
    string Status,
    decimal JurosPago,
    decimal AmortizacaoPaga,
    decimal SaldoDevedorApos
);