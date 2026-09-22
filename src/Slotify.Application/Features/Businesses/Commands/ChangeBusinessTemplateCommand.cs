using MediatR;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Commands;

/// <summary>
/// Comando para migrar un negocio a una nueva plantilla de sector (US-013).
/// </summary>
public record ChangeBusinessTemplateCommand(Guid BusinessId, int NewTemplateId) : IRequest<bool>;

public class ChangeBusinessTemplateCommandHandler(
    IBusinessRepository businessRepository,
    ISectorTemplateRepository sectorTemplateRepository,
    IUnitOfWork unitOfWork) 
    : IRequestHandler<ChangeBusinessTemplateCommand, bool>
{
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly ISectorTemplateRepository _sectorTemplateRepository = sectorTemplateRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<bool> Handle(ChangeBusinessTemplateCommand request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business == null)
            throw new Exception("Business not found");

        var newTemplate = await _sectorTemplateRepository.GetByIdAsync(request.NewTemplateId, cancellationToken);
        if (newTemplate == null)
            throw new Exception("Sector Template not found");

        business.SectorTemplateId = request.NewTemplateId;

        await _businessRepository.UpdateAsync(business, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
