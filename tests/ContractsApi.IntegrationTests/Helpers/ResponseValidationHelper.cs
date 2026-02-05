using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace ContractsApi.IntegrationTests.Helpers;

/// <summary>
/// Helper para validações comuns de respostas HTTP em testes
/// </summary>
public static class ResponseValidationHelper
{
    /// <summary>
    /// Valida que a resposta tem um status code esperado
    /// </summary>
    public static void ValidateStatusCode(HttpStatusCode actual, HttpStatusCode expected)
    {
        actual.Should().Be(expected);
    }

    /// <summary>
    /// Obtém o conteúdo da resposta como string
    /// </summary>
    public static async Task<string> GetResponseContentAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Extrai o JsonElement da resposta
    /// </summary>
    public static async Task<JsonElement> GetResponseJsonAsync(HttpResponseMessage response)
    {
        var content = await GetResponseContentAsync(response);
        return JsonResponseHelper.Deserialize(content);
    }

    /// <summary>
    /// Valida uma resposta de sucesso (OK)
    /// </summary>
    public static async Task<JsonElement> ValidateOkResponseAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await GetResponseJsonAsync(response);
    }

    /// <summary>
    /// Valida uma resposta de criação (Created)
    /// </summary>
    public static async Task<JsonElement> ValidateCreatedResponseAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await GetResponseJsonAsync(response);
    }

    /// <summary>
    /// Valida uma resposta de sucesso sem conteúdo (NoContent)
    /// </summary>
    public static void ValidateNoContentResponse(HttpStatusCode statusCode)
    {
        statusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Valida uma resposta de erro (BadRequest)
    /// </summary>
    public static void ValidateBadRequestResponse(HttpStatusCode statusCode)
    {
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Valida uma resposta de não encontrado (NotFound)
    /// </summary>
    public static void ValidateNotFoundResponse(HttpStatusCode statusCode)
    {
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Valida uma resposta de não autorizado (Unauthorized)
    /// </summary>
    public static void ValidateUnauthorizedResponse(HttpStatusCode statusCode)
    {
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Obtém a propriedade "data" de uma resposta JSON
    /// </summary>
    public static JsonElement GetDataFromResponse(JsonElement responseJson)
    {
        return responseJson.GetData();
    }

    /// <summary>
    /// Obtém o array de dados de uma resposta JSON
    /// </summary>
    public static JsonElement GetDataArrayFromResponse(JsonElement responseJson)
    {
        return responseJson.GetDataArray();
    }
}
