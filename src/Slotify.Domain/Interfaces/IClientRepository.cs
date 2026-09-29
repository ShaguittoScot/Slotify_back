namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio especializado para clientes consumidores (B2C).
/// </summary>
public interface IClientRepository : IRepository<Client>
{
    /// <summary>
    /// Busca un cliente por su correo electrónico.
    /// </summary>
    Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca un cliente por su ID de Supabase Auth.
    /// </summary>
    Task<Client?> GetBySupabaseIdAsync(Guid supabaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un correo ya está registrado para un cliente.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
