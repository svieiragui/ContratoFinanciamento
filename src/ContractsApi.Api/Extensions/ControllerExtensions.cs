using Microsoft.AspNetCore.Mvc;

namespace ContractsApi.Api.Extensions;

public static class ControllerExtensions
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// Obtém o correlation ID do header da requisição ou gera um novo se não existir.
    /// </summary>
    public static string GetOrGenerateCorrelationId(this ControllerBase controller)
    {
        if (controller.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationIdValue))
        {
            return correlationIdValue.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
