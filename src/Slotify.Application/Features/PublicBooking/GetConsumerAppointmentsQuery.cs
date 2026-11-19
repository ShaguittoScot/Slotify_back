using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.PublicBooking;

public record GetConsumerAppointmentsQuery(Guid UserId, string? Email)
    : IRequest<Result<IReadOnlyList<ConsumerAppointmentDto>>>;

public sealed class GetConsumerAppointmentsHandler(
    IAppointmentRepository appointments,
    IClientRepository clients)
    : IRequestHandler<GetConsumerAppointmentsQuery, Result<IReadOnlyList<ConsumerAppointmentDto>>>
{
    public async Task<Result<IReadOnlyList<ConsumerAppointmentDto>>> Handle(
        GetConsumerAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var resolvedClient = request.UserId != Guid.Empty
            ? await clients.GetBySupabaseIdAsync(request.UserId, cancellationToken)
              ?? await clients.GetByIdAsync(request.UserId, cancellationToken)
            : null;

        var items = await appointments.GetByConsumerIdentityAsync(
            resolvedClient?.Id ?? request.UserId,
            request.Email,
            cancellationToken);

        var result = items
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a =>
            {
                var status = a.Status == AppointmentStatus.NoShow
                    ? "no_show"
                    : a.Status.ToString().ToLowerInvariant();
                return new ConsumerAppointmentDto(
                    a.Id,
                    a.Business?.Name ?? "Negocio Slotify",
                    a.Business?.Slug ?? string.Empty,
                    !string.IsNullOrWhiteSpace(a.ServiceTitle) ? a.ServiceTitle : "Cita programada",
                    a.StartTime,
                    a.EndTime,
                    status,
                    a.AgreedTotal);
            })
            .ToArray();

        return Result<IReadOnlyList<ConsumerAppointmentDto>>.Ok(result);
    }
}
