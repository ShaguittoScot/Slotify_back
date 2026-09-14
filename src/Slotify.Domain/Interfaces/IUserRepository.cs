namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio especializado para usuarios.
/// Extiende operaciones más allá del CRUD genérico.
/// Relacionado con: US-000 (búsqueda por email para login)
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Busca un usuario por su correo electrónico.
    /// Utilizado en el flujo de login (US-000).
    /// </summary>
    /// <param name="email">Correo del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario encontrado o null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un correo ya está registrado.
    /// Utilizado en el flujo de registro (US-000).
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
