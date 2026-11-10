
using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Application.Features.Onboarding.DTOs;

namespace Slotify.Application.Features.Onboarding.Commands;

/// <summary>
/// Comando para completar el onboarding de un negocio.
/// Recibe los datos del propietario, negocio, horarios y servicios.
/// </summary>
public record CompleteBusinessOnboardingCommand
    : IRequest<Result<AuthUserDto>>
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public required OnboardingBusinessDto Business { get; init; }

    public required List<OnboardingBusinessHourDto> BusinessHours { get; init; }

    public required List<OnboardingServiceDto> Services { get; init; }
}
