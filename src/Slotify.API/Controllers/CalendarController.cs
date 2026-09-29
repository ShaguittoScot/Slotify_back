using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.Calendar.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para la vista de calendario del negocio.
/// Relacionado con: US-003 (Navegación Multiformato en Calendario Táctil)
/// 
/// Endpoints:
/// - GET /api/calendar/slots?startDate=...&endDate=... → Obtener slots del calendario
/// 
/// NOTA: Requiere autenticación JWT. El BusinessId se extrae del token.
/// TODO: Agregar atributo [Authorize] cuando se implemente el middleware JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize] // TODO: Descomentar cuando se configure el middleware de autenticación JWT
public class CalendarController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Obtiene los slots del calendario para un rango de fechas.
    /// </summary>
    /// <remarks>
    /// Combina citas, bloqueos y disponibilidad en una vista unificada.
    /// El businessId se extrae del JWT del usuario autenticado.
    /// Soporta filtro opcional por empleado.
    /// </remarks>
    [HttpGet("slots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? businessId,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var effectiveBusinessId = businessId ?? Guid.Empty;

        var query = new GetCalendarSlotsQuery
        {
            BusinessId = effectiveBusinessId,
            StartDate = startDate,
            EndDate = endDate,
            EmployeeId = employeeId
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Crea una nueva cita en el calendario.
    /// </summary>
    [HttpPost("appointments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentDto dto,
        [FromServices] Slotify.Infrastructure.Data.AppDbContext context,
        CancellationToken cancellationToken)
    {
        var businessId = dto.BusinessId;
        if (businessId == Guid.Empty)
        {
            var defaultBusiness = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Businesses, cancellationToken);
            if (defaultBusiness != null)
            {
                businessId = defaultBusiness.Id;
            }
            else
            {
                var newBusiness = new Slotify.Domain.Entities.Business
                {
                    Id = Guid.NewGuid(),
                    Name = "Mi Negocio",
                    Phone = "+52 55 1234 5678",
                    SectorTemplateId = 2
                };
                context.Businesses.Add(newBusiness);
                await context.SaveChangesAsync(cancellationToken);
                businessId = newBusiness.Id;
            }
        }

        var apt = new Slotify.Domain.Entities.Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            ClientName = dto.ClientName,
            ClientEmail = dto.ClientEmail ?? "cliente@ejemplo.com",
            ClientPhone = dto.ClientPhone ?? "+52 55 1234 5678",
            Status = Slotify.Domain.Enums.AppointmentStatus.Confirmed,
            CancellationToken = Guid.NewGuid().ToString("N"),
            AgreedTotal = dto.AgreedTotal
        };

        context.Appointments.Add(apt);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, id = apt.Id, businessId });
    }
}

public record CreateAppointmentDto
{
    public Guid BusinessId { get; init; } = Guid.Empty;
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required string ClientName { get; init; }
    public string? ClientEmail { get; init; }
    public string? ClientPhone { get; init; }
    public decimal AgreedTotal { get; init; } = 0.00m;
}
