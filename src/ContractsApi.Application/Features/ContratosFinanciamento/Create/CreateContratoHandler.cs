using ContractsApi.Domain.Common;
using ContractsApi.Domain.Entities;
using ContractsApi.Domain.Repositories;
using FluentValidation;

namespace ContractsApi.Application.Features.ContratosFinanciamento.Create;

public class CreateContratoHandler
{
    private readonly IContratoFinanciamentoRepository _repository;
    private readonly IValidator<CreateContratoCommand> _validator;

    public CreateContratoHandler(
        IContratoFinanciamentoRepository repository,
        IValidator<CreateContratoCommand> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<Result<ContratoResponseDto>> Handle(CreateContratoCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
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