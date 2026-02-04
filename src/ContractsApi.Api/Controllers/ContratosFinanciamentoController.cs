using ContractsApi.Application.Features.ContratosFinanciamento.Create;
using ContractsApi.Application.Features.ContratosFinanciamento.Delete;
using ContractsApi.Application.Features.ContratosFinanciamento.GetAll;
using ContractsApi.Application.Features.ContratosFinanciamento.GetById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractsApi.Api.Controllers;

[ApiController]
[Route("api/contratos")]
[Authorize]
public class ContratosFinanciamentoController : ControllerBase
{
    private readonly CreateContratoHandler _createHandler;
    private readonly GetAllContratosHandler _getAllHandler;
    private readonly GetContratoByIdHandler _getByIdHandler;
    private readonly DeleteContratoHandler _deleteHandler;

    public ContratosFinanciamentoController(
        CreateContratoHandler createHandler,
        GetAllContratosHandler getAllHandler,
        GetContratoByIdHandler getByIdHandler,
        DeleteContratoHandler deleteHandler)
    {
        _createHandler = createHandler;
        _getAllHandler = getAllHandler;
        _getByIdHandler = getByIdHandler;
        _deleteHandler = deleteHandler;
    }

    /// <summary>
    /// Cria um novo contrato de financiamento
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContratoCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Lista todos os contratos de financiamento
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _getAllHandler.Handle(new GetAllContratosQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Busca um contrato de financiamento por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(new GetContratoByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Deleta um contrato de financiamento
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteHandler.Handle(new DeleteContratoCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return NoContent();
    }
}