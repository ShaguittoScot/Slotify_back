namespace Slotify.Application.Common.Interfaces;

/// <summary>
/// Contrato para hashing y verificación de contraseñas.
/// Relacionado con: US-000 (Registro y Login)
/// 
/// IMPLEMENTACIÓN PENDIENTE en Infrastructure/Services/PasswordHasher.cs
/// Debe usar BCrypt (ya está como dependencia en Infrastructure.csproj).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Genera un hash seguro de la contraseña en texto plano.
    /// Usado al registrar un nuevo usuario (US-000).
    /// </summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Hash BCrypt de la contraseña.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifica que una contraseña en texto plano corresponda al hash almacenado.
    /// Usado en el flujo de login (US-000).
    /// </summary>
    /// <param name="password">Contraseña proporcionada por el usuario.</param>
    /// <param name="passwordHash">Hash almacenado en la BD.</param>
    /// <returns>true si la contraseña es correcta.</returns>
    bool Verify(string password, string passwordHash);
}
