using System.Text;
using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Clients.DTOs;
using Slotify.Application.Features.Clients.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Clients.Commands;

public class ManageBusinessClientHandler(
    IBusinessClientRepository clients,
    IAppointmentRepository appointments,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<ManageBusinessClientCommand, Result<BusinessClientDto?>>
{
    public async Task<Result<BusinessClientDto?>> Handle(
        ManageBusinessClientCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result<BusinessClientDto?>.Fail("El usuario no tiene un negocio asignado.", ErrorType.Forbidden);
        }

        var hasClientId = request.ClientId is { } clientId && clientId != Guid.Empty;
        var hasAppointmentId = request.AppointmentId is { } appointmentId && appointmentId != Guid.Empty;
        if (hasClientId == hasAppointmentId ||
            (hasAppointmentId && request.Action is not (BusinessClientAction.Archive or BusinessClientAction.UpdateNote)))
        {
            return Result<BusinessClientDto?>.Fail("Indica una ficha o una cita del historial.", ErrorType.Validation);
        }

        var note = request.Note?.Trim();
        if (request.Action == BusinessClientAction.UpdateNote && (note is null || note.Length > 1000))
        {
            return Result<BusinessClientDto?>.Fail("La nota debe tener hasta 1000 caracteres.", ErrorType.Validation);
        }

        var registered = (await clients.GetByBusinessIdAsync(businessId, cancellationToken))
            .Where(c => c.BusinessId == businessId)
            .ToList();
        var businessAppointments = (await appointments.GetByBusinessIdAsync(businessId, cancellationToken))
            .Where(a => a.BusinessId == businessId)
            .ToList();

        var portfolio = GetBusinessClientsHandler.BuildPortfolio(registered, businessAppointments);
        var snapshot = hasClientId
            ? portfolio.FirstOrDefault(c => c.IsRegistered && c.ClientId == request.ClientId)
            : portfolio.FirstOrDefault(c => c.Appointments.Any(a => a.Id == request.AppointmentId));

        if (snapshot is null)
        {
            return Result<BusinessClientDto?>.NotFound("La ficha no existe en la cartera de este negocio.");
        }

        var client = snapshot.IsRegistered ? registered.Single(c => c.Id == snapshot.ClientId) : null;
        switch (request.Action)
        {
            case BusinessClientAction.Delete:
                if (client is null || snapshot.Appointments.Count > 0)
                {
                    return Result<BusinessClientDto?>.Fail("La ficha tiene citas. Puedes archivarla para conservar su historial.", ErrorType.Conflict);
                }

                await clients.DeleteAsync(client, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<BusinessClientDto?>.Ok(null, "Ficha eliminada.");

            case BusinessClientAction.Restore:
                if (client is { ArchivedAt: not null })
                {
                    client.ArchivedAt = null;
                    await clients.UpdateAsync(client, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                snapshot.ArchivedAt = null;
                return Result<BusinessClientDto?>.Ok(snapshot, "Cliente restaurado.");

            case BusinessClientAction.UpdateNote when client?.ArchivedAt is not null:
                return Result<BusinessClientDto?>.Fail("Restaura la ficha antes de editar la nota.", ErrorType.Conflict);

            case BusinessClientAction.Archive:
            case BusinessClientAction.UpdateNote:
                var archive = request.Action == BusinessClientAction.Archive;
                var historyIds = snapshot.Appointments.Select(a => a.Id).Order().ToArray();
                if (client is null)
                {
                    // El mismo grupo histórico produce la misma ficha aun si llegan dos peticiones a la vez.
                    client = new BusinessClient
                    {
                        Id = historyIds[0],
                        BusinessId = businessId,
                        ClientId = snapshot.ClientId,
                        FirstName = LimitLength(snapshot.FirstName, 100),
                        LastName = LimitLength(snapshot.LastName, 100),
                        Email = LimitLength(snapshot.Email, 150),
                        Phone = LimitLength(snapshot.Phone, 30),
                        HistoryAppointmentIds = historyIds,
                        ArchivedAt = archive ? DateTime.UtcNow : null,
                        Notes = archive ? null : note,
                    };
                    await clients.AddAsync(client, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else if (!archive || client.ArchivedAt is null)
                {
                    if (archive)
                    {
                        client.ArchivedAt = DateTime.UtcNow;
                        client.HistoryAppointmentIds = historyIds;
                    }
                    else
                    {
                        client.Notes = note;
                    }
                    await clients.UpdateAsync(client, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                snapshot.Id = client.Id.ToString("D");
                snapshot.ClientId = client.Id;
                snapshot.IsRegistered = true;
                snapshot.ArchivedAt = client.ArchivedAt;
                snapshot.Notes = client.Notes;
                return Result<BusinessClientDto?>.Ok(snapshot, archive ? "Cliente archivado." : "Nota guardada.");

            default:
                return Result<BusinessClientDto?>.Fail("Acción no válida.", ErrorType.Validation);
        }
    }

    private static string LimitLength(string value, int length) =>
        value.Length <= length ? value : string.Concat(value.EnumerateRunes().Take(length));
}
