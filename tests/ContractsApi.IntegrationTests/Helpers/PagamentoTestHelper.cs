using FluentAssertions;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Helpers;

/// <summary>
/// Helper para criar e gerenciar pagamentos nos testes
/// </summary>
public static class PagamentoTestHelper
{
    /// <summary>
    /// Cria um pagamento com parâmetros padrão
    /// </summary>
    public static object CreateDefaultPagamentoRequest(
        int numeroParcela = 1,
        decimal valorPago = 1346.18m,
        DateTime? dataPagamento = null)
    {
        return new
        {
            numeroParcela,
            valorPago,
            dataPagamento = dataPagamento ?? DateTime.Today.AddDays(30)
        };
    }

    /// <summary>
    /// Cria um pagamento na API via HTTP
    /// </summary>
    public static async Task<Guid> CreatePagamentoAsync(
        HttpClient client,
        Guid contratoId,
        int numeroParcela = 1,
        decimal valorPago = 1346.18m,
        DateTime? dataPagamento = null)
    {
        var request = CreateDefaultPagamentoRequest(numeroParcela, valorPago, dataPagamento);
        var response = await client.PostAsJsonAsync($"/api/contratos/{contratoId}/pagamentos", request);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonResponseHelper.Deserialize(content);

        return result.GetData().GetGuidValue("id");
    }

    /// <summary>
    /// Cria múltiplos pagamentos em sequência
    /// </summary>
    public static async Task CreateMultiplePagamentosAsync(
        HttpClient client,
        Guid contratoId,
        int quantidadeParcelas,
        DateTime dataVencimentoPrimeiraParcela,
        decimal valorParcela = 1346.18m)
    {
        for (int i = 1; i <= quantidadeParcelas; i++)
        {
            var dataPagamento = dataVencimentoPrimeiraParcela.AddMonths(i - 1);
            await CreatePagamentoAsync(client, contratoId, i, valorParcela, dataPagamento);
        }
    }

    /// <summary>
    /// Valida as propriedades básicas de um pagamento criado
    /// </summary>
    public static void ValidatePagamentoResponse(
        JsonElement data,
        Guid expectedContratoId,
        int expectedNumeroParcela)
    {
        data.GetGuidValue("id").Should().NotBeEmpty();
        data.GetGuidValue("contratoId").Should().Be(expectedContratoId);
        data.GetIntValue("numeroParcela").Should().Be(expectedNumeroParcela);
    }

    /// <summary>
    /// Valida o status de um pagamento
    /// </summary>
    public static void ValidatePagamentoStatus(
        JsonElement data,
        string expectedStatus)
    {
        data.GetStringValue("status").Should().Be(expectedStatus);
    }
}
