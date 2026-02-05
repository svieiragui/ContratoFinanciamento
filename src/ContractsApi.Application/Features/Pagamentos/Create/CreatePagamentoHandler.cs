using ContractsApi.Application.Features.Pagamentos.Notifications;
using ContractsApi.Domain.Common;
using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContractsApi.Application.Features.Pagamentos.Create;

public class CreatePagamentoHandler
{
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly IContratoFinanciamentoRepository _contratoRepository;
    private readonly IValidator<CreatePagamentoCommand> _validator;
    private readonly IPublisher _publisher;
    private readonly ILogger<CreatePagamentoHandler> _logger;

    public CreatePagamentoHandler(
        IPagamentoRepository pagamentoRepository,
        IContratoFinanciamentoRepository contratoRepository,
        IValidator<CreatePagamentoCommand> validator,
        IPublisher publisher,
        ILogger<CreatePagamentoHandler> logger)
    {
        _pagamentoRepository = pagamentoRepository;
        _contratoRepository = contratoRepository;
        _validator = validator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<PagamentoResponseDto>> Handle(
        CreatePagamentoCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando CreatePagamento - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}, NumeroParcela: {NumeroParcela}",
            command.CorrelationId, command.ContratoId, command.NumeroParcela);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogError("Falha: Validação falhou - CorrelationId: {CorrelationId}, Erros: {Errors}",
                command.CorrelationId, errors);
            return Result<PagamentoResponseDto>.Failure(errors, 400);
        }

        // Verificar se contrato existe
        var contrato = await _contratoRepository.GetByIdAsync(command.ContratoId, cancellationToken);
        if (contrato == null)
        {
            _logger.LogError("Falha: Contrato não encontrado - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
                command.CorrelationId, command.ContratoId);
            return Result<PagamentoResponseDto>.Failure("Contrato não encontrado", 404);
        }

        // Verificar se parcela já foi paga
        var pagamentoExistente = await _pagamentoRepository.GetByContratoAndParcelaAsync(
            command.ContratoId,
            command.NumeroParcela,
            cancellationToken);

        if (pagamentoExistente != null)
        {
            _logger.LogError("Falha: Parcela já foi paga - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}, NumeroParcela: {NumeroParcela}",
                command.CorrelationId, command.ContratoId, command.NumeroParcela);
            return Result<PagamentoResponseDto>.Failure("Parcela já foi paga", 409);
        }

        // Validar número da parcela
        if (command.NumeroParcela > contrato.PrazoMeses)
        {
            _logger.LogError("Falha: Número da parcela inválido - CorrelationId: {CorrelationId}, NumeroParcela: {NumeroParcela}, PrazoMeses: {PrazoMeses}",
                command.CorrelationId, command.NumeroParcela, contrato.PrazoMeses);
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

        var notification = new PagamentoRegistradoNotification(
            PagamentoId: pagamento.Id,
            ContratoId: pagamento.ContratoId,
            NumeroParcela: pagamento.NumeroParcela,
            ValorPago: pagamento.ValorPago,
            AmortizacaoPaga: pagamento.AmortizacaoPaga,
            DataRegistro: DateTime.UtcNow
        );

        await _publisher.Publish(notification, cancellationToken);

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

        _logger.LogInformation("CreatePagamento finalizado com sucesso - CorrelationId: {CorrelationId}, PagamentoId: {PagamentoId}",
            command.CorrelationId, pagamento.Id);

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