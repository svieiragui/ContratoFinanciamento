using FluentValidation;

namespace ContractsApi.Application.Features.Pagamentos.Create;

public class CreatePagamentoValidator : AbstractValidator<CreatePagamentoCommand>
{
    public CreatePagamentoValidator()
    {
        RuleFor(x => x.ContratoId)
            .NotEmpty().WithMessage("ID do contrato é obrigatório");

        RuleFor(x => x.NumeroParcela)
            .GreaterThan(0).WithMessage("Número da parcela deve ser maior que zero");

        RuleFor(x => x.ValorPago)
            .GreaterThan(0).WithMessage("Valor pago deve ser maior que zero");

        RuleFor(x => x.DataPagamento)
            .NotEmpty().WithMessage("Data de pagamento é obrigatória");
    }
}