using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;

namespace Slotify.Application.Features.Businesses.Queries;

/// <summary>
/// Query CQRS para consultar la información y configuración completa del negocio
/// perteneciente al propietario autenticado.
/// No recibe identificadores desde el cliente; resuelve el negocio a través de la identidad
/// autenticada en el token JWT y las relaciones persistidas en la base de datos.
/// </summary>
public record GetMyBusinessProfileQuery : IRequest<Result<BusinessProfileDto>>;
