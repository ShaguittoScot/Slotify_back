using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Application.Features.Onboarding.DTOs;

namespace Slotify.Application.Features.Onboarding.Commands;

/// <summary>
/// Comando para completar la configuración de onboarding de un negocio.
/// Contiene exclusivamente los datos funcionales del negocio, horarios, servicios y perfil.
/// La identidad del usuario y la pertenencia del negocio se resuelven exclusivamente desde el contexto autenticado (JWT).
/// </summary>
public record CompleteBusinessOnboardingCommand
    : IRequest<Result<AuthUserDto>>
{
    public required string FirstName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public required OnboardingBusinessDto Business { get; init; }

    public required List<OnboardingBusinessHourDto> BusinessHours { get; init; }

    public required List<OnboardingServiceDto> Services { get; init; }
}
