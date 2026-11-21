
using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;

namespace Slotify.Application.Features.Businesses.Queries;

public record GetBusinessesQuery()
    : IRequest<Result<IEnumerable<BusinessListItemDto>>>;
