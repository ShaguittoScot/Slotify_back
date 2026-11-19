using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Notifications.DTOs;

namespace Slotify.Application.Features.Notifications.Queries;

/// <summary>
/// Query para obtener las citas próximas del usuario autenticado (negocio o cliente).
/// US-Notifications: Motor de notificaciones push — polling de snapshot al arrancar la app.
/// 
/// Reglas:
/// - Si el usuario tiene businessId → devuelve citas del negocio (vista admin/empleado).
/// - Si el usuario es CLIENTE → devuelve sus propias citas reservadas.
/// - HorizonDays: número de días hacia adelante a consultar (default 30).
/// </summary>
public record GetUpcomingNotificationsQuery : IRequest<Result<UpcomingNotificationsResponse>>
{
    /// <summary>Número de días hacia adelante a consultar (1–90, default 30).</summary>
    public int HorizonDays { get; init; } = 30;
}
