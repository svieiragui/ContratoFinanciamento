using FluentValidation;

namespace ContractsApi.Application.Features.ContratosFinanciamento.Create;

public class CreateContratoValidator : AbstractValidator<CreateContratoCommand>
{
    public CreateContratoValidator()
    {
        RuleFor(x => x.ClienteCpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ é obrigatório")
            .SetValidator(new CpfCnpjValidator()).WithMessage("CPF ou CNPJ inválido");

        RuleFor(x => x.ValorTotal)
            .GreaterThan(0).WithMessage("Valor total deve ser maior que zero");

        RuleFor(x => x.TaxaMensal)
            .GreaterThan(0).WithMessage("Taxa mensal deve ser maior que zero")
            .LessThanOrEqualTo(10).WithMessage("Taxa mensal não pode exceder 10%");

        RuleFor(x => x.PrazoMeses)
            .GreaterThan(0).WithMessage("Prazo deve ser maior que zero")
            .LessThanOrEqualTo(84).WithMessage("Prazo máximo é de 84 meses");

        RuleFor(x => x.DataVencimentoPrimeiraParcela)
            .GreaterThan(DateTime.Today).WithMessage("Data da primeira parcela deve ser futura");

        RuleFor(x => x.TipoVeiculo)
            .IsInEnum().WithMessage("Tipo de veículo inválido");

        RuleFor(x => x.CondicaoVeiculo)
            .IsInEnum().WithMessage("Condição de veículo inválida");
    }
}