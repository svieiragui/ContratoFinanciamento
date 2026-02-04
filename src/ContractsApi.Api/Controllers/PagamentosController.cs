using ContractsApi.Application.Features.Pagamentos.Create;
using ContractsApi.Application.Features.Pagamentos.GetByContrato;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractsApi.Api.Controllers;

[ApiController]
[Route("api/contratos/{contratoId}/pagamentos")]
[Authorize]
public class PagamentosController : ControllerBase
{
    private readonly CreatePagamentoHandler _createHandler;
    private readonly GetPagamentosByContratoHandler _getByContratoHandler;

    public PagamentosController(
        CreatePagamentoHandler createHandler,
        GetPagamentosByContratoHandler getByContratoHandler)
    {
        _createHandler = createHandler;
        _getByContratoHandler = getByContratoHandler;
    }

    /// <summary>
    /// Registra um pagamento de parcela
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid contratoId,
        [FromBody] CreatePagamentoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePagamentoCommand(
            contratoId,
            request.NumeroParcela,
            request.ValorPago,
            request.DataPagamento
        );

        var result = await _createHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Lista todos os pagamentos de um contrato
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetByContrato(Guid contratoId, CancellationToken cancellationToken)
    {
        var result = await _getByContratoHandler.Handle(
            new GetPagamentosByContratoQuery(contratoId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }
}

public record CreatePagamentoRequest(
    int NumeroParcela,
    decimal ValorPago,
    DateTime DataPagamento
);