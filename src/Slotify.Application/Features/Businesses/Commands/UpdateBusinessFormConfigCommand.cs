using FluentValidation;
using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using System.Text.Json;

namespace Slotify.Application.Features.Businesses.Commands;

public record UpdateBusinessFormConfigCommand(Guid BusinessId, string FormConfigJson) : IRequest<Result<bool>>;

public class UpdateBusinessFormConfigValidator : AbstractValidator<UpdateBusinessFormConfigCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UpdateBusinessFormConfigValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("El identificador del negocio es obligatorio.");

        RuleFor(x => x.FormConfigJson)
            .NotEmpty().WithMessage("La configuración del formulario es obligatoria.")
            .Must(BeValidFormAndBrandingJson)
            .WithMessage("La configuración del formulario o los colores corporativos/logotipo no tienen un formato válido.");
    }

    private static bool BeValidFormAndBrandingJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            var config = JsonSerializer.Deserialize<BookingFormConfigDto>(json, JsonOptions);
            if (config == null)
                return false;

            if (!string.IsNullOrWhiteSpace(config.AccentColor) &&
                !Business.IsValidCorporateHexColor(config.AccentColor))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(config.SecondaryColor) &&
                !Business.IsValidCorporateHexColor(config.SecondaryColor))
            {
                return false;
            }

            if (!Business.IsValidLogoSource(config.LogoUrl))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class UpdateBusinessFormConfigCommandHandler(
    IBusinessRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBusinessFormConfigCommand, Result<bool>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string InvalidConfigMessage =
        "No se pudo actualizar la configuración. Verifica el formato de los colores corporativos y del logotipo.";

    private readonly IBusinessRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<bool>> Handle(
        UpdateBusinessFormConfigCommand request,
        CancellationToken cancellationToken)
    {
        var business = await _repository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business == null)
            return Result<bool>.NotFound("Negocio no encontrado.");

        // Validar JSON e identidad corporativa (Zero Trust)
        BookingFormConfigDto? parsedConfig;
        try
        {
            parsedConfig = JsonSerializer.Deserialize<BookingFormConfigDto>(
                request.FormConfigJson,
                JsonOptions);

            if (parsedConfig == null)
                return Result<bool>.Fail(InvalidConfigMessage);
        }
        catch
        {
            return Result<bool>.Fail(InvalidConfigMessage);
        }

        if (!Business.IsValidCorporateHexColor(parsedConfig.AccentColor))
            return Result<bool>.Fail(InvalidConfigMessage);

        if (!string.IsNullOrWhiteSpace(parsedConfig.SecondaryColor) &&
            !Business.IsValidCorporateHexColor(parsedConfig.SecondaryColor))
        {
            return Result<bool>.Fail(InvalidConfigMessage);
        }

        if (!Business.IsValidLogoSource(parsedConfig.LogoUrl))
            return Result<bool>.Fail(InvalidConfigMessage);

        if (!string.IsNullOrWhiteSpace(parsedConfig.BusinessName))
        {
            business.Name = parsedConfig.BusinessName.Trim();
        }

        var normalizedConfig = parsedConfig with
        {
            AccentColor = parsedConfig.AccentColor.Trim().ToUpperInvariant(),
            SecondaryColor = string.IsNullOrWhiteSpace(parsedConfig.SecondaryColor)
                ? parsedConfig.AccentColor.Trim().ToUpperInvariant()
                : parsedConfig.SecondaryColor.Trim().ToUpperInvariant(),
            BusinessName = string.IsNullOrWhiteSpace(parsedConfig.BusinessName)
                ? business.Name
                : parsedConfig.BusinessName.Trim()
        };

        business.CustomFormConfig = JsonSerializer.Serialize(normalizedConfig, JsonOptions);
        business.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(business, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }
}
