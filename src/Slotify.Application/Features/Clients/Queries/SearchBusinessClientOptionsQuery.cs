using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Clients.Queries;

public record SearchBusinessClientOptionsQuery(string? Search) : IRequest<Result<IReadOnlyList<BusinessClientOption>>>;

public sealed class SearchBusinessClientOptionsHandler(IBusinessClientRepository clients, ICurrentUserService currentUser)
    : IRequestHandler<SearchBusinessClientOptionsQuery, Result<IReadOnlyList<BusinessClientOption>>>
{
    public async Task<Result<IReadOnlyList<BusinessClientOption>>> Handle(
        SearchBusinessClientOptionsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.BusinessId is not { } businessId || businessId == Guid.Empty)
            return Result<IReadOnlyList<BusinessClientOption>>.Fail("El usuario no tiene un negocio asignado.", ErrorType.Forbidden);

        var term = request.Search?.Trim();
        if (term is null || term.Length < 2)
            return Result<IReadOnlyList<BusinessClientOption>>.Ok([]);
        if (term.Length > 150)
            return Result<IReadOnlyList<BusinessClientOption>>.Fail("La búsqueda es demasiado larga.", ErrorType.Validation);

        return Result<IReadOnlyList<BusinessClientOption>>.Ok(
            await clients.SearchActiveAsync(businessId, term, cancellationToken));
    }
}
