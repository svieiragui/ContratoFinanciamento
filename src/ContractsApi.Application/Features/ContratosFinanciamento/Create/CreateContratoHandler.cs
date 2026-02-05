using ContractsApi.Domain.Common;
using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ContractsApi.Application.Features.ContratosFinanciamento.Create;

public class CreateContratoHandler
{
    private readonly IContratoFinanciamentoRepository _repository;
    private readonly IValidator<CreateContratoCommand> _validator;
    private readonly ILogger<CreateContratoHandler> _logger;

    public CreateContratoHandler(
        IContratoFinanciamentoRepository repository,
        IValidator<CreateContratoCommand> validator,
        ILogger<CreateContratoHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ContratoResponseDto>> Handle(CreateContratoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando CreateContrato - CorrelationId: {CorrelationId}, ClienteCpfCnpj: {ClienteCpfCnpj}",
            command.CorrelationId, command.ClienteCpfCnpj);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogError("Falha: Validação falhou - CorrelationId: {CorrelationId}, Erros: {Errors}",
                command.CorrelationId, errors);
            return Result<ContratoResponseDto>.Failure(errors, 400);
        }

        var contrato = ContratoFinanciamento.Create(
            command.ClienteCpfCnpj,
            command.ValorTotal,
            command.TaxaMensal,
            command.PrazoMeses,
            command.DataVencimentoPrimeiraParcela,
            command.TipoVeiculo,
            command.CondicaoVeiculo
        );

        var id = await _repository.CreateAsync(contrato, cancellationToken);

        var response = new ContratoResponseDto(
            id,
            contrato.ClienteCpfCnpj,
            contrato.ValorTotal,
            contrato.TaxaMensal,
            contrato.PrazoMeses,
            contrato.DataVencimentoPrimeiraParcela,
            contrato.TipoVeiculo.ToString(),
            contrato.CondicaoVeiculo.ToString(),
            contrato.Status.ToString(),
            contrato.ValorParcela,
            contrato.SaldoDevedor
        );

        _logger.LogInformation("CreateContrato finalizado com sucesso - CorrelationId: {CorrelationId}, ContratoId: {ContratoId}",
            command.CorrelationId, id);

        return Result<ContratoResponseDto>.Success(response, 201);
    }
}

public record ContratoResponseDto(
    Guid Id,
    string ClienteCpfCnpj,
    decimal ValorTotal,
    decimal TaxaMensal,
    int PrazoMeses,
    DateTime DataVencimentoPrimeiraParcela,
    string TipoVeiculo,
    string CondicaoVeiculo,
    string Status,
    decimal ValorParcela,
    decimal SaldoDevedor
);