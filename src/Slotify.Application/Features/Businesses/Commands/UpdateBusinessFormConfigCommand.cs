using MediatR;
using Slotify.Domain.Interfaces;
using System.Text.Json;

namespace Slotify.Application.Features.Businesses.Commands;

public record UpdateBusinessFormConfigCommand(Guid BusinessId, string FormConfigJson) : IRequest<bool>;

public class UpdateBusinessFormConfigCommandHandler(IBusinessRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBusinessFormConfigCommand, bool>
{
    private readonly IBusinessRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<bool> Handle(UpdateBusinessFormConfigCommand request, CancellationToken cancellationToken)
    {
        var business = await _repository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business == null)
            return false;

        // Validar que el JSON es válido
        try
        {
            JsonDocument.Parse(request.FormConfigJson);
        }
        catch
        {
            return false;
        }

        business.CustomFormConfig = request.FormConfigJson;
        await _repository.UpdateAsync(business, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
