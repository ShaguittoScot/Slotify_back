using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Commands;

/// <summary>
/// Comando para migrar un negocio a una nueva plantilla de sector (US-013).
/// </summary>
public record ChangeBusinessTemplateCommand(Guid BusinessId, int NewTemplateId) : IRequest<Result<bool>>;

public class ChangeBusinessTemplateCommandHandler(
    IBusinessRepository businessRepository,
    ISectorTemplateRepository sectorTemplateRepository,
    IUnitOfWork unitOfWork) 
    : IRequestHandler<ChangeBusinessTemplateCommand, Result<bool>>
{
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly ISectorTemplateRepository _sectorTemplateRepository = sectorTemplateRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<bool>> Handle(ChangeBusinessTemplateCommand request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business == null)
            return Result<bool>.NotFound("Business not found");

        var newTemplate = await _sectorTemplateRepository.GetByIdAsync(request.NewTemplateId, cancellationToken);
        if (newTemplate == null)
            return Result<bool>.NotFound("Sector Template not found");

        business.SectorTemplateId = request.NewTemplateId;

        await _businessRepository.UpdateAsync(business, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}
