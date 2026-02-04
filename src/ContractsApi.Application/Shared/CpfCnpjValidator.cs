using FluentValidation;

namespace ContractsApi.Application.Features.ContratosFinanciamento.Create;

public class CpfCnpjValidator : AbstractValidator<string>
{
    public CpfCnpjValidator()
    {
        RuleFor(x => x)
            .Custom((value, context) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    context.AddFailure("CPF ou CNPJ é obrigatório");
                    return;
                }

                var onlyNumbers = new string(value.Where(char.IsDigit).ToArray());

                if (onlyNumbers.Length == 11 && !ValidateCpf(onlyNumbers))
                {
                    context.AddFailure("CPF inválido");
                }
                else if (onlyNumbers.Length == 14 && !ValidateCnpj(onlyNumbers))
                {
                    context.AddFailure("CNPJ inválido");
                }
                else if (onlyNumbers.Length != 11 && onlyNumbers.Length != 14)
                {
                    context.AddFailure("CPF deve ter 11 dígitos ou CNPJ 14 dígitos");
                }
            });
    }

    private static bool ValidateCpf(string cpf)
    {
        if (cpf.All(c => c == cpf[0]))
            return false;

        var digits = cpf.Select(c => int.Parse(c.ToString())).ToArray();
        var sum = 0;

        for (int i = 0; i < 9; i++)
            sum += digits[i] * (10 - i);

        var remainder = sum % 11;
        var firstVerifier = remainder < 2 ? 0 : 11 - remainder;

        if (digits[9] != firstVerifier)
            return false;

        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += digits[i] * (11 - i);

        remainder = sum % 11;
        var secondVerifier = remainder < 2 ? 0 : 11 - remainder;

        return digits[10] == secondVerifier;
    }

    private static bool ValidateCnpj(string cnpj)
    {
        if (cnpj.All(c => c == cnpj[0]))
            return false;

        var digits = cnpj.Select(c => int.Parse(c.ToString())).ToArray();
        var multiplier = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum = 0;

        for (int i = 0; i < 12; i++)
            sum += digits[i] * multiplier[i];

        var remainder = sum % 11;
        var firstVerifier = remainder < 2 ? 0 : 11 - remainder;

        if (digits[12] != firstVerifier)
            return false;

        multiplier = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        sum = 0;

        for (int i = 0; i < 13; i++)
            sum += digits[i] * multiplier[i];

        remainder = sum % 11;
        var secondVerifier = remainder < 2 ? 0 : 11 - remainder;

        return digits[13] == secondVerifier;
    }
}