using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Clients.DTOs;

namespace Slotify.Application.Features.Clients.Commands;

public enum BusinessClientAction
{
    Archive,
    Restore,
    Delete,
    UpdateNote
}

public record ManageBusinessClientCommand(
    BusinessClientAction Action,
    Guid? ClientId,
    Guid? AppointmentId = null,
    string? Note = null) : IRequest<Result<BusinessClientDto?>>;
