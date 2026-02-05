using ContractsApi.Api.Extensions;
using ContractsApi.Application.Features.Clientes.GetResumo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractsApi.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly GetResumoClienteHandler _getResumoHandler;

    public ClientesController(GetResumoClienteHandler getResumoHandler)
    {
        _getResumoHandler = getResumoHandler;
    }

    /// <summary>
    /// Retorna o resumo consolidado de um cliente por CPF/CNPJ
    /// </summary>
    [HttpGet("{cpfCnpj}/resumo")]
    public async Task<IActionResult> GetResumo(string cpfCnpj, CancellationToken cancellationToken)
    {
        var correlationId = this.GetOrGenerateCorrelationId();

        var result = await _getResumoHandler.Handle(
            new GetResumoClienteQuery(cpfCnpj, correlationId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage, correlationId });
        }

        return Ok(new { data = result.Data, correlationId });
    }
}