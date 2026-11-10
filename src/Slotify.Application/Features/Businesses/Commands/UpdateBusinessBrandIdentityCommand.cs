using FluentValidation;
using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using System.Text.Json;

namespace Slotify.Application.Features.Businesses.Commands;

/// <summary>
/// Comando CQRS para cargar el logotipo y definir los colores corporativos principales
/// del negocio sobre el formulario público de reservas.
/// </summary>
public record UpdateBusinessBrandIdentityCommand(
    Guid BusinessId,
    string? LogoUrl,
    string AccentColor,
    string? SecondaryColor,
    string? BusinessName = null) : IRequest<Result<BookingFormConfigDto>>;

public class UpdateBusinessBrandIdentityValidator : AbstractValidator<UpdateBusinessBrandIdentityCommand>
{
    public UpdateBusinessBrandIdentityValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("El ID del negocio es obligatorio.");

        RuleFor(x => x.AccentColor)
            .NotEmpty().WithMessage("El color corporativo primario es obligatorio.")
            .Must(Business.IsValidCorporateHexColor)
            .WithMessage("El color corporativo primario debe ser un código hexadecimal válido (#RGB o #RRGGBB).");

        RuleFor(x => x.SecondaryColor)
            .Must(c => string.IsNullOrWhiteSpace(c) || Business.IsValidCorporateHexColor(c))
            .WithMessage("El color corporativo secundario debe ser un código hexadecimal válido (#RGB o #RRGGBB).");

        RuleFor(x => x.LogoUrl)
            .Must(Business.IsValidLogoSource)
            .WithMessage("El logotipo debe ser una URL válida (http/https) o una imagen codificada en Base64 menor a 4 MB.");

        RuleFor(x => x.BusinessName)
            .MaximumLength(150).WithMessage("El nombre comercial no puede exceder 150 caracteres.");
    }
}

public class UpdateBusinessBrandIdentityCommandHandler(
    IBusinessRepository businessRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService? currentUserService = null)
    : IRequestHandler<UpdateBusinessBrandIdentityCommand, Result<BookingFormConfigDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService? _currentUserService = currentUserService;

    public async Task<Result<BookingFormConfigDto>> Handle(
        UpdateBusinessBrandIdentityCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService is not null && !BelongsToAuthenticatedBusiness(request.BusinessId))
            return Result<BookingFormConfigDto>.Fail("No tienes permisos para modificar otro negocio.", ErrorType.Forbidden);

        if (!Business.IsValidCorporateHexColor(request.AccentColor))
            return Result<BookingFormConfigDto>.Fail("El color corporativo primario no es un código hexadecimal válido.");

        if (!string.IsNullOrWhiteSpace(request.SecondaryColor) &&
            !Business.IsValidCorporateHexColor(request.SecondaryColor))
        {
            return Result<BookingFormConfigDto>.Fail("El color corporativo secundario no es un código hexadecimal válido.");
        }

        if (!Business.IsValidLogoSource(request.LogoUrl))
            return Result<BookingFormConfigDto>.Fail("El formato del logotipo no es válido.");

        var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business == null)
            return Result<BookingFormConfigDto>.NotFound("Negocio no encontrado.");

        BookingFormConfigDto currentConfig = new();
        if (!string.IsNullOrWhiteSpace(business.CustomFormConfig))
        {
            try
            {
                currentConfig = JsonSerializer.Deserialize<BookingFormConfigDto>(
                    business.CustomFormConfig,
                    JsonOptions) ?? new BookingFormConfigDto();
            }
            catch
            {
                currentConfig = new BookingFormConfigDto();
            }
        }

        if (!string.IsNullOrWhiteSpace(request.BusinessName))
        {
            business.Name = request.BusinessName.Trim();
        }

        var primaryHex = request.AccentColor.Trim().ToUpperInvariant();
        var secondaryHex = string.IsNullOrWhiteSpace(request.SecondaryColor)
            ? primaryHex
            : request.SecondaryColor.Trim().ToUpperInvariant();

        var updatedConfig = currentConfig with
        {
            LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
            AccentColor = primaryHex,
            SecondaryColor = secondaryHex,
            BusinessName = business.Name
        };

        business.CustomFormConfig = JsonSerializer.Serialize(updatedConfig, JsonOptions);
        if (business.Brand != null)
        {
            business.Brand.LogoUrl = updatedConfig.LogoUrl;
            business.Brand.PrimaryColor = updatedConfig.AccentColor;
            business.Brand.IsDefault = false;
            business.Brand.UpdatedAt = DateTime.UtcNow;
        }
        business.UpdatedAt = DateTime.UtcNow;

        await _businessRepository.UpdateAsync(business, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BookingFormConfigDto>.Ok(updatedConfig, "Identidad de marca actualizada correctamente.");
    }

    private bool BelongsToAuthenticatedBusiness(Guid targetBusinessId) =>
        targetBusinessId != Guid.Empty
        && _currentUserService?.BusinessId is { } tokenBusinessId
        && tokenBusinessId != Guid.Empty
        && tokenBusinessId == targetBusinessId;
}
