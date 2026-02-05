using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Domain.Enums;
using FluentAssertions;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Helpers;

/// <summary>
/// Helper para criar e gerenciar contratos nos testes
/// </summary>
public static class ContratoTestHelper
{
    /// <summary>
    /// Cria um contrato com parâmetros padrão
    /// </summary>
    public static CreateContratoCommand CreateDefaultContratoCommand(
        string cpfCnpj = "12345678901",
        decimal valorTotal = 50000,
        decimal taxaMensal = 2.5m,
        int prazoMeses = 48,
        DateTime? dataVencimentoPrimeiraParcela = null,
        TipoVeiculo tipoVeiculo = TipoVeiculo.AUTOMOVEL,
        CondicaoVeiculo condicaoVeiculo = CondicaoVeiculo.NOVO)
    {
        return new CreateContratoCommand(
            ClienteCpfCnpj: cpfCnpj,
            ValorTotal: valorTotal,
            TaxaMensal: taxaMensal,
            PrazoMeses: prazoMeses,
            DataVencimentoPrimeiraParcela: dataVencimentoPrimeiraParcela ?? DateTime.Today.AddDays(30),
            TipoVeiculo: tipoVeiculo,
            CondicaoVeiculo: condicaoVeiculo,
            CorrelationId: Guid.NewGuid().ToString()
        );
    }

    /// <summary>
    /// Cria um contrato na API via HTTP
    /// </summary>
    public static async Task<Guid> CreateContratoAsync(
        HttpClient client,
        string cpfCnpj = "12345678901",
        decimal valorTotal = 50000,
        decimal taxaMensal = 2.5m,
        int prazoMeses = 48,
        DateTime? dataVencimentoPrimeiraParcela = null)
    {
        var command = CreateDefaultContratoCommand(cpfCnpj, valorTotal, taxaMensal, prazoMeses, dataVencimentoPrimeiraParcela);
        var response = await client.PostAsJsonAsync("/api/contratos", command);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonResponseHelper.Deserialize(content);
        
        return result.GetData().GetGuidValue("id");
    }

    /// <summary>
    /// Valida as propriedades básicas de um contrato criado
    /// </summary>
    public static void ValidateContratoResponse(
        JsonElement data,
        string expectedCpfCnpj,
        decimal expectedValorTotal)
    {
        data.GetGuidValue("id").Should().NotBeEmpty();
        data.GetStringValue("clienteCpfCnpj").Should().Be(expectedCpfCnpj);
        data.GetDecimalValue("valorTotal").Should().Be(expectedValorTotal);
        data.GetDecimalValue("valorParcela").Should().BeGreaterThan(0);
        data.GetDecimalValue("saldoDevedor").Should().Be(expectedValorTotal);
    }
}
