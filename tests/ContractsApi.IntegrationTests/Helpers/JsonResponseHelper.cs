using System.Text.Json;

namespace ContractsApi.IntegrationTests.Helpers;

/// <summary>
/// Helper para desserialização e extração de dados de respostas JSON
/// </summary>
public static class JsonResponseHelper
{
    /// <summary>
    /// Desserializa uma string JSON em JsonElement
    /// </summary>
    public static JsonElement Deserialize(string content)
    {
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    /// <summary>
    /// Extrai uma propriedade do primeiro nível da resposta
    /// </summary>
    public static JsonElement GetProperty(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName);
    }

    /// <summary>
    /// Extrai o valor de uma propriedade string
    /// </summary>
    public static string GetStringValue(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetString() ?? string.Empty;
    }

    /// <summary>
    /// Extrai o valor de uma propriedade int
    /// </summary>
    public static int GetIntValue(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetInt32();
    }

    /// <summary>
    /// Extrai o valor de uma propriedade decimal
    /// </summary>
    public static decimal GetDecimalValue(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetDecimal();
    }

    /// <summary>
    /// Extrai o valor de uma propriedade Guid
    /// </summary>
    public static Guid GetGuidValue(this JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetGuid();
    }

    /// <summary>
    /// Obtém a propriedade "data" da resposta
    /// </summary>
    public static JsonElement GetData(this JsonElement element)
    {
        return element.GetProperty("data");
    }

    /// <summary>
    /// Obtém o array de dados da resposta
    /// </summary>
    public static JsonElement GetDataArray(this JsonElement element)
    {
        return element.GetProperty("data");
    }
}
