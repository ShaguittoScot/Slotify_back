using MediatR;
using Slotify.Application.Common.Models;

namespace Slotify.Application.Features.Calendar.Commands;

/// <summary>
/// Comando para agendar una nueva cita en el calendario.
/// Relacionado con: US-003 / US-011 (Agendamiento de Citas).
/// </summary>
public record CreateAppointmentCommand : IRequest<Result<Guid>>
{
    public Guid BusinessId { get; init; } = Guid.Empty;
    public Guid? EmployeeId { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required string ClientName { get; init; }
    public string? ClientEmail { get; init; }
    public string? ClientPhone { get; init; }
    public decimal AgreedTotal { get; init; } = 0.00m;
}
