using ContractsApi.Domain.Enums;

namespace ContractsApi.Application.Features.ContratosFinanciamento.Create;

public record CreateContratoCommand(
    string ClienteCpfCnpj,
    decimal ValorTotal,
    decimal TaxaMensal,
    int PrazoMeses,
    DateTime DataVencimentoPrimeiraParcela,
    TipoVeiculo TipoVeiculo,
    CondicaoVeiculo CondicaoVeiculo,
    string CorrelationId
);