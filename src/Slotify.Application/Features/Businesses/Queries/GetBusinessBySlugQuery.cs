using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;

namespace Slotify.Application.Features.Businesses.Queries;

/// <summary>
/// Consulta para obtener el perfil y catálogo público de un negocio mediante su slug.
/// </summary>
public record GetBusinessBySlugQuery(string Slug) : IRequest<Result<BusinessDetailDto>>;
