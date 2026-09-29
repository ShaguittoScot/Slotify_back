namespace Slotify.Application.Features.Auth.DTOs;

/// <summary>
/// DTO con datos del cliente retornado tras registro / consulta.
/// </summary>
public record ClientDto
{
    public required Guid Id { get; init; }
    public Guid? SupabaseId { get; init; }
    public required string FirstName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}
