using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Features.Clients.Commands;
using Slotify.Application.Features.Clients.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para la gestión de la cartera de clientes e historial de citas del negocio.
/// Relacionado con: US-026 (Construcción de módulo de clientes) y US-022 (Aislamiento por BusinessId del JWT).
/// 
/// Endpoints:
/// - GET /api/clients?search=... → Obtener cartera de clientes del negocio autenticado
/// - POST /api/clients → Registrar o actualizar un cliente en la cartera del negocio
/// - PUT /api/clients/{id} → Actualizar datos de contacto y notas de un cliente específico del negocio
/// </summary>
[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("options")]
    public async Task<IActionResult> SearchOptions([FromQuery] string? search, CancellationToken cancellationToken) =>
        (await _mediator.Send(new SearchBusinessClientOptionsQuery(search), cancellationToken)).ToActionResult();

    /// <summary>
    /// Obtiene la cartera de clientes del negocio autenticado junto con sus métricas e historial de citas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClients(
        [FromQuery] string? search,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var query = new GetBusinessClientsQuery { Search = search, IncludeArchived = includeArchived };
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Registra o actualiza un cliente en la cartera del negocio autenticado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertClient(
        [FromBody] UpsertBusinessClientCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Actualiza los datos de contacto y notas de un cliente existente del negocio por su ID.
    /// Retorna 404 si el ID no existe o no pertenece al negocio autenticado.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClient(
        [FromRoute] Guid id,
        [FromBody] UpsertBusinessClientCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command with { ClientId = id, IsExplicitUpdateById = true },
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("note")]
    public async Task<IActionResult> UpdateNote([FromBody] UpdateClientNoteRequest request, CancellationToken cancellationToken) =>
        (await _mediator.Send(new ManageBusinessClientCommand(
            BusinessClientAction.UpdateNote, request.ClientId, request.AppointmentId, request.Note), cancellationToken)).ToActionResult();

    [HttpPost("archive")]
    public async Task<IActionResult> ArchiveClient(
        [FromBody] ArchiveClientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ManageBusinessClientCommand(BusinessClientAction.Archive, request.ClientId, request.AppointmentId),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreClient(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ManageBusinessClientCommand(BusinessClientAction.Restore, id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClient(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ManageBusinessClientCommand(BusinessClientAction.Delete, id), cancellationToken);
        return result.ToActionResult();
    }

    public record ArchiveClientRequest(Guid? ClientId, Guid? AppointmentId);
    public record UpdateClientNoteRequest(Guid? ClientId, Guid? AppointmentId, string? Note);
}
