using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.SectorTemplates.DTOs;

namespace Slotify.Application.Features.SectorTemplates.Queries;

/// <summary>
/// Query para obtener todas las plantillas de sector disponibles.
/// Relacionado con: US-006 (Selector Inicial de Giro Comercial)
/// 
/// Se usa cuando el admin está configurando su negocio por primera vez
/// y necesita seleccionar un giro comercial.
/// Los datos vienen de seed data (pre-cargados).
/// </summary>
public record GetSectorTemplatesQuery : IRequest<Result<List<SectorTemplateDto>>>;
