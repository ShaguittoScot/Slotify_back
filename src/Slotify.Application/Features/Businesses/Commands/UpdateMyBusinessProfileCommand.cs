using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;

namespace Slotify.Application.Features.Businesses.Commands;

/// <summary>
/// Comando CQRS para actualizar la información general y ubicación física del negocio
/// perteneciente al propietario autenticado.
/// Resuelve el negocio exclusivamente a través de la identidad del usuario en sesión (Zero Trust).
/// </summary>
public record UpdateMyBusinessProfileCommand(
    string Name,
    string? Phone = null,
    string? Description = null,
    string? Street = null,
    string? ExteriorNumber = null,
    string? InteriorNumber = null,
    string? Neighborhood = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? CountryCode = null
) : IRequest<Result<BusinessProfileDto>>;
